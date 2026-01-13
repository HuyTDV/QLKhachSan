using Microsoft.AspNetCore.Mvc;
using QLKhachSan.Services;

namespace QLKhachSan.Controllers
{
    public class ChatController : Controller
    {
        private readonly AiChatService _aiChatService;

        public ChatController(AiChatService aiChatService)
        {
            _aiChatService = aiChatService;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message)) return BadRequest(new { reply = "Tin nhắn trống" });

                // --- SỬA ĐOẠN NÀY ---
                // 1. Lưu tạm một cái gì đó vào Session để "khóa" ID lại, không cho nó đổi cái mới
                HttpContext.Session.SetString("UserSession", "Active");

                string sessionId = HttpContext.Session.Id;

                // (Debug) In ra xem ID có bị đổi không
                Console.WriteLine($"👉 Chat Session ID: {sessionId}");
                // --------------------

                var response = await _aiChatService.ProcessChat(message, sessionId);
                return Ok(new { reply = response });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi: {ex.Message}");
                return StatusCode(500, new { reply = "Lỗi server." });
            }
        }
    }
}