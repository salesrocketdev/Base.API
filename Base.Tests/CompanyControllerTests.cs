using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Base.API.Controllers;
using Base.API.DTOs;
using Base.Domain.Entities;
using Base.Domain.Interfaces;

namespace Base.Tests;

public class CompanyControllerTests
{
    [Fact]
    public async Task UpdateById_ReturnsForbidden_WhenRequestedCompanyIsNotTenantCompany()
    {
        var service = new FakeCompanyService();
        var controller = CreateController(service, role: "Owner", companyPublicId: Guid.NewGuid());

        var result = await controller.Update(Guid.NewGuid(), new CompanyUpdateRequest("Updated", null));

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.False(service.UpdateCalled);
    }

    [Fact]
    public async Task UpdateById_CallsService_WhenRequestedCompanyMatchesTenant()
    {
        var service = new FakeCompanyService();
        var companyId = Guid.NewGuid();
        var controller = CreateController(service, role: "Admin", companyPublicId: companyId);

        var result = await controller.Update(companyId, new CompanyUpdateRequest("Updated", "{}"));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        Assert.True(service.UpdateCalled);
    }

    private static CompanyController CreateController(FakeCompanyService service, string role, Guid companyPublicId)
    {
        var http = new DefaultHttpContext();
        http.Items["UserRole"] = role;
        http.Items["CompanyPublicId"] = companyPublicId;
        http.Items["CompanyId"] = 1;

        return new CompanyController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    private sealed class FakeCompanyService : ICompanyService
    {
        public bool UpdateCalled { get; private set; }

        public Task<Company> CreateAsync(string name, string? settingsJson = null)
            => Task.FromResult(new Company { Name = name });

        public Task<Company?> GetByIdAsync(Guid publicId)
            => Task.FromResult<Company?>(new Company { PublicId = publicId, Name = "Base" });

        public Task<Company?> GetByIdWithMembersAsync(Guid publicId)
            => Task.FromResult<Company?>(new Company { PublicId = publicId, Name = "Base" });

        public Task UpdateAsync(Guid publicId, string name, string? settingsJson = null)
        {
            UpdateCalled = true;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid publicId) => Task.CompletedTask;

        public Task<Company?> GetByCompanyIdWithMembersAsync(int companyId)
            => Task.FromResult<Company?>(new Company { Id = companyId, PublicId = Guid.NewGuid(), Name = "Base" });

        public Task UpdateByCompanyIdAsync(int companyId, string name, string? settingsJson = null)
            => Task.CompletedTask;

        public Task<CompanyMember> InviteMemberAsync(int companyId, string email, string role)
            => Task.FromResult(new CompanyMember { CompanyId = companyId, UserId = 1, Role = role });
    }
}
