using System.ComponentModel.DataAnnotations;

namespace QLKhachSan.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        public string FullName { get; set; } = null!;

        [Required]
        public string Username { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
