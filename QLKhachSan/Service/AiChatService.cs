using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;

namespace QLKhachSan.Services
{
    public class AiChatService
    {
        private readonly string _connectionString;
        private readonly string _geminiApiKey;
        private readonly string _geminiApiUrl;

        // --- BỘ NHỚ TẠM (Chỉ khai báo 1 lần duy nhất, KHÔNG trùng lặp) ---
        private static Dictionary<string, List<string>> _chatHistory = new();

        // --- SCHEMA DATABASE ĐẦY ĐỦ ---
        private const string DbSchema = @"
        Bạn là chuyên gia SQL và Lễ tân khách sạn Grandora.
        
        1. DATABASE SCHEMA:
        - Rooms(RoomId, BranchId, RoomNumber, RoomType, Capacity, Price, Amenities, Status, ImageUrl)
          + Status: 'Available' (Trống), 'Booked' (Đã đặt), 'Maintenance' (Bảo trì).
        - HotelBranches(BranchId, HotelId, BranchName, Address, City, Country, Phone, ImageUrl)
        - Hotels(HotelId, HotelName, Rating, Description, Hotline, Email)
        - Bookings(BookingId, UserId, RoomId, CheckIn, CheckOut, TotalPrice, Status, ServicesUsed)
        - Payments(PaymentId, BookingId, Amount, PaymentMethod, TransactionCode, PaidAt, PromotionCode)
        - Users(UserId, FullName, Username, Email, Phone, Role, Address)
        - Services(ServiceId, ServiceName, Description, Price, ImageUrl)
        - Promotions(PromotionId, Code, Description, DiscountPercent, StartDate, EndDate)
        - Reviews(ReviewId, UserId, RoomId, Rating, Comment, Reply)
        - BlogPosts(PostId, Title, Content, AuthorId, ImageUrl)
        - Galleries(GalleryId, Title, ImageUrl, HotelId)
        - Menus(MenuId, MenuName, Url, Icon, Role, ImageUrl)
        - RoomMaintenance(MaintenanceId, RoomId, Description, MaintenanceDate, Status)

        LIÊN KẾT: Rooms.BranchId = HotelBranches.BranchId | Bookings.RoomId = Rooms.RoomId

        2. QUY TẮC SQL (BẮT BUỘC):
        - Chỉ trả về 1 lệnh SELECT.
        - Tìm phòng LUÔN lấy: RoomId, RoomNumber, RoomType, Price, ImageUrl, Amenities.
        - Tìm phòng trống phải có: Rooms.Status = 'Available'.
        
        3. QUY TẮC NO_SQL (QUAN TRỌNG):
        - Nếu câu hỏi là Xã giao (hi, chào) HOẶC Hỏi lịch trình du lịch/ăn uống -> Trả về: NO_SQL
        - LƯU Ý ĐẶC BIỆT: Nếu khách nhờ tư vấn (VD: 'nên đặt phòng nào', 'gợi ý phòng', 'có phòng nào hợp không') -> KHÔNG ĐƯỢC trả về NO_SQL.
          -> Hãy phân tích Lịch sử chat (ngân sách, số người) để tạo câu lệnh SQL tìm phòng phù hợp.
        ";

        private const string LocalKnowledge = @"
        Bạn là Lễ tân khách sạn Grandora tại TP. Vinh, Nghệ An.
        THÔNG TIN BỔ SUNG:
        - Giá phòng: 500k - 5 triệu.
        - DU LỊCH: Biển Cửa Lò (15km), Quê Bác (13km), Đảo Chè Thanh Chương (40km).
        - ẨM THỰC: Cháo lươn, Súp lươn Nghệ An.
        ";

        public AiChatService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new Exception("Chuỗi kết nối không tìm thấy");
            _geminiApiKey = configuration["Gemini:ApiKey"];
            // Dùng model 2.5 Flash
            _geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_geminiApiKey}";
        }

        public async Task<string> ProcessChat(string userMessage, string sessionId)
        {
            string historyContext = GetHistory(sessionId);

            // Bước 1: Sinh SQL
            string sqlQuery = await GenerateSqlFromText(userMessage, historyContext);
            string botReply = "";

            if (sqlQuery.Contains("NO_SQL") || sqlQuery.StartsWith("ERROR") || string.IsNullOrWhiteSpace(sqlQuery))
            {
                botReply = await AskGeminiGeneral(userMessage, historyContext);
            }
            else
            {
                // Bước 2: Chạy SQL và hiển thị Card phòng (Có nút Đặt Ngay)
                string dataResult = ExecuteSql(sqlQuery);
                botReply = await GenerateNaturalResponse(userMessage, dataResult, historyContext);
            }

            // Bước 3: Lưu lịch sử chat
            SaveHistory(sessionId, "User: " + userMessage);
            string cleanReply = System.Text.RegularExpressions.Regex.Replace(botReply, "<.*?>", String.Empty);
            SaveHistory(sessionId, "Bot: " + cleanReply);

            return botReply;
        }

        // --- CÁC HÀM PHỤ TRỢ ---
        private string GetHistory(string sessionId)
        {
            if (!_chatHistory.ContainsKey(sessionId)) return "";
            return string.Join("\n", _chatHistory[sessionId].TakeLast(6));
        }

        private void SaveHistory(string sessionId, string msg)
        {
            if (!_chatHistory.ContainsKey(sessionId)) _chatHistory[sessionId] = new List<string>();
            _chatHistory[sessionId].Add(msg);
            if (_chatHistory[sessionId].Count > 10) _chatHistory[sessionId].RemoveAt(0);
        }

        private async Task<string> GenerateSqlFromText(string question, string history)
        {
            var prompt = $"{DbSchema}\n\nLỊCH SỬ CHAT (Chứa nhu cầu khách):\n{history}\n\nCâu hỏi: {question}\nSQL Query:";

            var response = await CallGeminiApi(prompt);
            response = response.Replace("```sql", "").Replace("```", "").Trim();

            if (response.Contains("NO_SQL")) return "NO_SQL";
            if (!response.ToUpper().StartsWith("SELECT")) return "ERROR";
            return response;
        }

        private string ExecuteSql(string sql)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var result = conn.Query(sql);
                    if (!result.Any()) return "EMPTY_RESULT_SET";
                    return JsonConvert.SerializeObject(result);
                }
            }
            catch (Exception ex) { return $"SQL_ERROR: {ex.Message}"; }
        }

        private async Task<string> GenerateNaturalResponse(string question, string data, string history)
        {
            var prompt = $@"
            {LocalKnowledge}
            ----------------
            LỊCH SỬ CHAT: {history}
            ----------------
            Câu hỏi: '{question}'
            Dữ liệu DB: {data}

            YÊU CẦU:
            1. Trả lời ngắn gọn: 'Dựa trên nhu cầu của anh chị, em tìm thấy các phòng này...'
            2. QUAN TRỌNG: Hiển thị HTML Card cho từng phòng (Mẫu dưới, KHÔNG ĐƯỢC THAY ĐỔI):

            <div class='card d-inline-block mb-2 mr-2 shadow-sm border-0' style='width: 250px; vertical-align: top; background: #fff; border-radius: 12px;'>
                <div style='position: relative;'>
                    <img src='/assets/img/hotel/{{ImageUrl}}' class='card-img-top' style='height: 140px; object-fit: cover; border-top-left-radius: 12px; border-top-right-radius: 12px;' onerror=""this.src='https://placehold.co/600x400?text=Grandora'"">
                    <span class='badge badge-success' style='position: absolute; top: 10px; right: 10px;'>{{RoomType}}</span>
                </div>
                <div class='card-body p-2 text-left'>
                    <h6 class='card-title font-weight-bold text-dark mb-1'>P.{{RoomNumber}}</h6>
                    <p class='text-danger font-weight-bold mb-1' style='font-size: 1.1em;'>{{Price}} ₫</p>
                    <p class='text-muted small mb-2 text-truncate'>{{Amenities}}</p>
                    
                    <a href='/Home/Booking?roomId={{RoomId}}' class='btn btn-sm btn-primary btn-block' style='border-radius: 20px; width: 100%;'>Đặt Ngay</a>
                </div>
            </div>
            
            Chỉ trả về Text/HTML.
            ";
            return await CallGeminiApi(prompt);
        }

        private async Task<string> AskGeminiGeneral(string question, string history)
        {
            var prompt = $@"
            {LocalKnowledge}
            ----------------
            LỊCH SỬ CHAT: {history}
            ----------------
            Khách hỏi: '{question}'
            YÊU CẦU: Trả lời tiếp nối ngữ cảnh. Gợi ý lịch trình du lịch nếu được hỏi.
            ";
            return await CallGeminiApi(prompt);
        }

        private async Task<string> CallGeminiApi(string promptText)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var requestBody = new { contents = new[] { new { parts = new[] { new { text = promptText } } } } };
                    var response = await client.PostAsync(_geminiApiUrl, new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"));
                    var jsonResponse = JObject.Parse(await response.Content.ReadAsStringAsync());
                    return jsonResponse["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? "AI không phản hồi.";
                }
            }
            catch (Exception ex) { return $"Lỗi hệ thống: {ex.Message}"; }
        }
    }
}