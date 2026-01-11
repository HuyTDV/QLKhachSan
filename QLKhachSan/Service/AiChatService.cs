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

        // Cấu trúc Database (Schema)
        private const string DbSchema = @"
        Bạn là chuyên gia SQL. Database khách sạn Grandora:
        
        1. BẢNG DỮ LIỆU:
        - Rooms(RoomId, RoomNumber, BranchId, RoomType, Capacity, Price, Amenities, Status, ImageUrl)
          + Status: 'Available', 'Booked', 'Maintenance'
        - HotelBranches(BranchId, BranchName, City)
        - Bookings(BookingId, UserId, RoomId, CheckIn, CheckOut, TotalPrice, Status)

        QUY TẮC SQL (BẮT BUỘC):
        1. Chỉ trả về 1 câu lệnh SELECT. Không giải thích. Không Markdown.
        2. Nếu tìm phòng, LUÔN LUÔN lấy các cột: RoomId, RoomNumber, RoomType, Price, ImageUrl, Amenities.
        3. Tìm phòng trống phải có điều kiện: Status = 'Available'.
        
        4. QUAN TRỌNG NHẤT: Nếu câu hỏi là xã giao (Ví dụ: 'hi', 'xin chào', 'bạn là ai', 'cảm ơn') HOẶC câu hỏi không cần tra cứu dữ liệu (Ví dụ: 'tư vấn cho tôi', 'khách sạn có đẹp không') -> HÃY TRẢ VỀ DUY NHẤT CHỮ: NO_SQL
        ";

        public AiChatService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new Exception("Chuỗi kết nối không tìm thấy");

            _geminiApiKey = configuration["Gemini:ApiKey"];
            _geminiApiUrl =
    $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={_geminiApiKey}";

        }

        public async Task<string> ProcessChat(string userMessage)
        {
            string sqlQuery = await GenerateSqlFromText(userMessage);

            // SỬA Ở ĐÂY: Thêm check điều kiện "NO_SQL"
            if (sqlQuery.Contains("NO_SQL") || sqlQuery.StartsWith("ERROR") || string.IsNullOrWhiteSpace(sqlQuery))
            {
                return await AskGeminiGeneral(userMessage);
            }

            string dataResult = ExecuteSql(sqlQuery);
            return await GenerateNaturalResponse(userMessage, dataResult);
        }

        private async Task<string> GenerateSqlFromText(string question)
        {
            var prompt = $"{DbSchema}\n\nCâu hỏi: {question}\nSQL Query:";
            var response = await CallGeminiApi(prompt);

            response = response.Replace("```sql", "").Replace("```", "").Trim();

            // Nếu AI trả về NO_SQL thì return luôn
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
                    if (!result.Any()) return "EMPTY_DATA";
                    return JsonConvert.SerializeObject(result);
                }
            }
            catch (Exception ex) { return $"Lỗi truy vấn: {ex.Message}"; }
        }

        private async Task<string> GenerateNaturalResponse(string question, string data)
        {
            if (data == "EMPTY_DATA")
            {
                return "Dạ hiện tại em không tìm thấy phòng nào phù hợp với yêu cầu của anh/chị ạ.";
            }

            var prompt = $@"
            Dữ liệu phòng tìm được (JSON): {data}
            Câu hỏi của khách: '{question}'

            YÊU CẦU TRẢ LỜI:
            1. Đóng vai lễ tân, trả lời ngắn gọn (dưới 20 từ) ở dòng đầu tiên.
            2. Sau đó, hiển thị danh sách phòng dưới dạng HTML Card.
            3. Sử dụng mẫu HTML sau cho MỖI phòng (Không được thay đổi cấu trúc class, chỉ thay nội dung trong {{...}}):

            <div class='card d-inline-block m-1 shadow-sm' style='width: 260px; vertical-align: top; border: 1px solid #eee;'>
                <img src='/images/rooms/{{ImageUrl}}' class='card-img-top' style='height: 150px; object-fit: cover; width: 100%;' onerror=""this.src='https://placehold.co/600x400?text=No+Image'"">
                <div class='card-body p-2'>
                    <h6 class='card-title text-primary font-weight-bold mb-1'>{{RoomType}} - P.{{RoomNumber}}</h6>
                    <p class='text-danger font-weight-bold mb-1' style='font-size: 1.1em;'>{{Price}} VNĐ</p>
                    <p class='text-muted small mb-2' style='height: 40px; overflow: hidden;'>{{Amenities}}</p>
                    <a href='/Home/Booking?roomId={{RoomId}}' class='btn btn-sm btn-success btn-block' style='width: 100%;'>Đặt ngay</a>
                </div>
            </div>

            LƯU Ý: 
            - Thay thế {{ImageUrl}}, {{RoomType}}, {{Price}}... bằng dữ liệu thật từ JSON.
            - Nếu ImageUrl chỉ là tên file, giữ nguyên đường dẫn '/images/rooms/filename.jpg'.
            - KHÔNG dùng Markdown, chỉ trả về text và mã HTML thô.
            ";

            return await CallGeminiApi(prompt);
        }

        private async Task<string> AskGeminiGeneral(string question)
        {
            return await CallGeminiApi($"Bạn là lễ tân khách sạn Grandora. Trả lời ngắn gọn, thân thiện câu hỏi: {question}. (Không bịa đặt thông tin phòng ốc nếu không biết).");
        }

        // Thay thế toàn bộ hàm CallGeminiApi cũ bằng hàm này
        private async Task<string> CallGeminiApi(string promptText)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var requestBody = new
                    {
                        contents = new[] { new { parts = new[] { new { text = promptText } } } }
                    };

                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(_geminiApiUrl, content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    // 1. Kiểm tra nếu HTTP Request thất bại (Ví dụ: 400 Bad Request, 401 Unauthorized...)
                    if (!response.IsSuccessStatusCode)
                    {
                        // Trả về lỗi chi tiết từ Google để dễ debug
                        return $"Lỗi Google API ({response.StatusCode}): {responseString}";
                    }

                    var jsonResponse = JObject.Parse(responseString);

                    // 2. Kiểm tra xem có nội dung trả về không
                    var text = jsonResponse["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                    // Nếu text vẫn null (có thể do bị chặn nội dung - Safety Filter)
                    if (string.IsNullOrEmpty(text))
                    {
                        return $"Google không trả lời. Chi tiết: {responseString}";
                    }

                    return text;
                }
            }
            catch (Exception ex)
            {
                return $"Lỗi hệ thống (C#): {ex.Message}";
            }
        }
    }
}