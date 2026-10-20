using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public interface IRepositoryBase<T> where T : class, IEntity
{
    IReadOnlyList<T> GetAll();
    T? GetById(int id);
    T Add(T entity);
    bool Update(T entity);
    bool Delete(int id);
}
