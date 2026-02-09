using System.ComponentModel.DataAnnotations;

namespace Base.API.DTOs;

public record InviteMemberRequest(
    [Required][EmailAddress] string Email,
    [Required] string Role
);

public record CompanyMemberResponse(Guid Id, Guid CompanyId, Guid UserId, string Role, DateTime CreatedAt);

