using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKhachSan.Services;

namespace QLKhachSan.Controllers
{
    [AllowAnonymous]
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
                if (string.IsNullOrWhiteSpace(message))
                    return BadRequest(new { reply = "Tin nhắn trống" });

                var response = await _aiChatService.ProcessChat(message);
                return Ok(new { reply = response });
            }
            catch (Exception ex)
            {
                // Log lỗi ra console để debug
                Console.WriteLine($"❌ Lỗi ChatController: {ex.Message}");
                return StatusCode(500, new { reply = $"Lỗi server: {ex.Message}" });
            }
        }
    }
}