using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Base.API.Constants;
using Base.API.DTOs;
using Base.Domain.Interfaces;

namespace Base.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompanyController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CompanyCreateRequest request)
    {
        var company = await _companyService.CreateAsync(request.Name, request.SettingsJson);
        var settings = company.Settings?.RootElement.GetRawText();
        var response = new CompanyResponse(company.PublicId, company.Name, settings, company.CreatedAt);
        return CreatedAtAction(nameof(GetById), new { id = company.PublicId }, ApiResponse<CompanyResponse>.Ok(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var company = await _companyService.GetByIdWithMembersAsync(id);
        if (company == null)
        {
            return NotFound(ApiResponse<object?>.Fail(
                "Company not found.",
                new ApiError(ApiErrorCodes.InvalidOperation, "Company not found.", ApiErrorTypes.NotFound)
            ));
        }

        var settings = company.Settings?.RootElement.GetRawText();
        var response = new CompanyResponse(company.PublicId, company.Name, settings, company.CreatedAt);
        return Ok(ApiResponse<CompanyResponse>.Ok(response));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrent()
    {
        var companyId = HttpContext.Items["CompanyId"] as int? ?? 0;
        if (companyId == 0)
        {
            return NotFound(ApiResponse<object?>.Fail(
                "Company not found.",
                new ApiError(ApiErrorCodes.InvalidOperation, "Company not found.", ApiErrorTypes.NotFound)
            ));
        }

        var company = await _companyService.GetByCompanyIdWithMembersAsync(companyId);
        if (company == null)
        {
            return NotFound(ApiResponse<object?>.Fail(
                "Company not found.",
                new ApiError(ApiErrorCodes.InvalidOperation, "Company not found.", ApiErrorTypes.NotFound)
            ));
        }

        var settings = company.Settings?.RootElement.GetRawText();
        var response = new CompanyResponse(company.PublicId, company.Name, settings, company.CreatedAt);
        return Ok(ApiResponse<CompanyResponse>.Ok(response));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateCurrent([FromBody] CompanyUpdateRequest request)
    {
        // Simple RBAC: only Owner or Admin can update company
        var role = HttpContext.Items["UserRole"] as string ?? string.Empty;
        if (role != "Owner" && role != "Admin")
        {
            return StatusCode(403, ApiResponse<object?>.Fail(
                "Forbidden.",
                new ApiError(ApiErrorCodes.Unauthorized, "Forbidden.", ApiErrorTypes.Unauthorized)
            ));
        }

        var companyId = HttpContext.Items["CompanyId"] as int? ?? 0;
        if (companyId == 0)
        {
            return NotFound(ApiResponse<object?>.Fail(
                "Company not found.",
                new ApiError(ApiErrorCodes.InvalidOperation, "Company not found.", ApiErrorTypes.NotFound)
            ));
        }

        await _companyService.UpdateByCompanyIdAsync(companyId, request.Name, request.SettingsJson);
        return Ok(ApiResponse<object?>.Ok(null, "Company updated successfully."));
    }

    [HttpPost("members/invite")]
    public async Task<IActionResult> InviteMember([FromBody] InviteMemberRequest request)
    {
        // Only Owner / Admin can invite
        var role = HttpContext.Items["UserRole"] as string ?? string.Empty;
        if (role != "Owner" && role != "Admin")
        {
            return StatusCode(403, ApiResponse<object?>.Fail(
                "Forbidden.",
                new ApiError(ApiErrorCodes.Unauthorized, "Forbidden.", ApiErrorTypes.Unauthorized)
            ));
        }

        var companyId = HttpContext.Items["CompanyId"] as int? ?? 0;
        if (companyId == 0)
        {
            return NotFound(ApiResponse<object?>.Fail(
                "Company not found.",
                new ApiError(ApiErrorCodes.InvalidOperation, "Company not found.", ApiErrorTypes.NotFound)
            ));
        }

        try
        {
            var created = await _companyService.InviteMemberAsync(companyId, request.Email, request.Role);
            var response = new CompanyMemberResponse(created.PublicId, created.Company?.PublicId ?? Guid.Empty, created.User?.PublicId ?? Guid.Empty, created.Role, created.CreatedAt);
            return Accepted(ApiResponse<CompanyMemberResponse>.Ok(response));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object?>.Fail(
                ex.Message,
                new ApiError(ApiErrorCodes.InvalidOperation, ex.Message, ApiErrorTypes.Validation)
            ));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CompanyUpdateRequest request)
    {
        var roleCheck = EnsureManagementRole();
        if (roleCheck != null)
        {
            return roleCheck;
        }

        var tenantCheck = EnsureTenantCompany(id);
        if (tenantCheck != null)
        {
            return tenantCheck;
        }

        await _companyService.UpdateAsync(id, request.Name, request.SettingsJson);
        return Ok(ApiResponse<object?>.Ok(null, "Company updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var roleCheck = EnsureManagementRole();
        if (roleCheck != null)
        {
            return roleCheck;
        }

        var tenantCheck = EnsureTenantCompany(id);
        if (tenantCheck != null)
        {
            return tenantCheck;
        }

        await _companyService.DeleteAsync(id);
        return Ok(ApiResponse<object?>.Ok(null, "Company deleted successfully."));
    }

    private IActionResult? EnsureManagementRole()
    {
        var role = HttpContext.Items["UserRole"] as string ?? string.Empty;
        if (role == "Owner" || role == "Admin")
        {
            return null;
        }

        return StatusCode(403, ApiResponse<object?>.Fail(
            "Forbidden.",
            new ApiError(ApiErrorCodes.Unauthorized, "Forbidden.", ApiErrorTypes.Unauthorized)
        ));
    }

    private IActionResult? EnsureTenantCompany(Guid requestedCompanyId)
    {
        var currentCompanyPublicId = HttpContext.Items["CompanyPublicId"] as Guid? ?? Guid.Empty;
        if (currentCompanyPublicId == Guid.Empty)
        {
            return NotFound(ApiResponse<object?>.Fail(
                "Company not found.",
                new ApiError(ApiErrorCodes.InvalidOperation, "Company not found.", ApiErrorTypes.NotFound)
            ));
        }

        if (currentCompanyPublicId == requestedCompanyId)
        {
            return null;
        }

        return StatusCode(403, ApiResponse<object?>.Fail(
            "Forbidden.",
            new ApiError(ApiErrorCodes.Unauthorized, "Forbidden.", ApiErrorTypes.Unauthorized)
        ));
    }
}

