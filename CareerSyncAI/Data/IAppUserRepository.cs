using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public interface IAppUserRepository : IRepositoryBase<AppUser>
{
    AppUser? FindByEmail(string email);
    bool EmailExists(string email);

    bool IsActiveUser(int id);
}
