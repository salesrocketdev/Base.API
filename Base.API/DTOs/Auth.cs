using System.ComponentModel.DataAnnotations;
using Base.API.Constants;

namespace Base.API.DTOs;

public record SignupRequest(
    [Required][EmailAddress] string Email,
    [Required][MinLength(ValidationConstants.PasswordMinLength)] string Password,
    string? Name
);

public record LoginRequest(
    [Required] string Email,
    [Required] string Password
);

public record LoginInitiateRequest(
    [Required][EmailAddress] string Email
);

public record RefreshRequest(
    [Required] string RefreshToken
);

public record LogoutRequest(
    [Required] string RefreshToken
);

public record ForgotPasswordRequest(
    [Required][EmailAddress] string Email
);

public record ResetPasswordRequest(
    [Required][EmailAddress] string Email,
    [Required][RegularExpression(@"^\d{6}$")]
    [Required][StringLength(ValidationConstants.OtpLength, MinimumLength = ValidationConstants.OtpLength)] string Otp,
    [Required][MinLength(ValidationConstants.PasswordMinLength)] string NewPassword
);

public record SwitchOrganizationRequest(
    [Required] Guid OrganizationId
);

public record FirstAccessCompleteRequest(
    [Required][EmailAddress] string Email,
    [Required][StringLength(ValidationConstants.OtpLength, MinimumLength = ValidationConstants.OtpLength)] string Otp,
    [Required][MinLength(ValidationConstants.PasswordMinLength)] string NewPassword,
    [Required][MinLength(ValidationConstants.PersonNameMinLength)] string FirstName,
    [Required][MinLength(ValidationConstants.PersonNameMinLength)] string LastName
);

public record FirstAccessResendRequest(
    [Required][EmailAddress] string Email
);

public record UserSummary(Guid Id, string Email, string? Name, string? AvatarUrl);

public record LoginResponse(string AccessToken, string RefreshToken, UserSummary User);

public record RefreshResponse(string AccessToken, string RefreshToken);

public record LoginInitiateResponse(string NextStep, string MaskedEmail);

public record SwitchOrganizationResponse(string AccessToken);
