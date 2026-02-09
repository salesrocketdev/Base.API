namespace Base.Core.Email.Models;

public class VerificationCodeModel
{
    public string Name { get; set; } = string.Empty;
    public string OTP { get; set; } = string.Empty;
}
