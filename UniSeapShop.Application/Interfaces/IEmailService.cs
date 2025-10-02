using UniSeapShop.Domain.DTOs.EmailDTOs;

namespace UniSeapShop.Application.Interfaces;

public interface IEmailService
{
    Task SendRegistrationSuccessEmailAsync(EmailRequestDto request);
    Task SendOtpVerificationEmailAsync(EmailRequestDto request);
    Task SendForgotPasswordOtpEmailAsync(EmailRequestDto request);
    Task SendPasswordChangeEmailAsync(EmailRequestDto request);
}