using CareerSyncAI.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerSyncAI.Data;

public class AppUserRepository : RepositoryBase<AppUser>, IAppUserRepository
{
    public AppUserRepository(CareerSyncDbContext context) : base(context) { }

    public AppUser? FindByEmail(string email) =>
        GetAll().FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

    public bool EmailExists(string email) => FindByEmail(email) != null;

    public bool IsActiveUser(int id) =>
        DbSet.AsNoTracking().Any(u => u.Id == id && u.IsActive);
}
