using System.ComponentModel.DataAnnotations;
using UniSeapShop.Domain.Enums;

namespace UniSeapShop.Domain.Entities
{
    public class OtpVerification : BaseEntity
    {
        [Required][MaxLength(255)] public string Target { get; set; } // Email hoặc SĐT

        [Required][MaxLength(10)] public string OtpCode { get; set; }

        public DateTime ExpiredAt { get; set; }

        public bool IsUsed { get; set; }

        public OtpPurpose Purpose { get; set; } // enum: Register, ForgotPassword, TwoFactor, etc.
    }
}
