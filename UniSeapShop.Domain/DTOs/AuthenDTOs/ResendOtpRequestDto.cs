using UniSeapShop.Domain.Enums;

namespace UniSeapShop.Domain.DTOs.AuthenDTOs
{
    public class ResendOtpRequestDto
    {
        public required string Email { get; set; }
        public OtpType Type { get; set; }
    }
}
