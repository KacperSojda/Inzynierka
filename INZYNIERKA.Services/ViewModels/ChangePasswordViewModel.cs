using System.ComponentModel.DataAnnotations;

namespace INZYNIERKA.Services.ViewModels
{
    public class ChangePasswordViewModel
    {
        public string Email { get; set; }

        [Required(ErrorMessage = "Enter the OTP code.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "The OTP code must be 6 digits.")]
        public string OtpCode { get; set; }

        [StringLength(40, MinimumLength = 8, ErrorMessage = "The {0} must be at {2} and at max {1} characters long.")]
        [Required(ErrorMessage = "Enter the new password.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
