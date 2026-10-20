using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public interface IMessageRepository : IRepositoryBase<Message>
{
    IReadOnlyList<Message> GetByGraduateId(int graduateId);
}
