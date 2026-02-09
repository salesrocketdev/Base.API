using Base.Domain.Entities;
using Base.Domain.Interfaces;
using Base.Infrastructure.Data;

namespace Base.Infrastructure.Repositories;

public class AppLogRepository : BaseRepository<AppLog>, IAppLogRepository
{
    public AppLogRepository(ApplicationDbContext context) : base(context) { }
}


