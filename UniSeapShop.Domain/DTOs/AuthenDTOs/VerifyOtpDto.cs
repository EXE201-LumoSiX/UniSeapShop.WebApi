﻿namespace UniSeapShop.Domain.DTOs.AuthenDTOs;

public class VerifyOtpDto
{
    public string? Email { get; set; }
    public string? Otp { get; set; }
}