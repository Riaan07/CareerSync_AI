using CareerSyncAI.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerSyncAI.Data;

public class RepositoryBase<T> : IRepositoryBase<T> where T : class, IEntity
{
    protected readonly CareerSyncDbContext Context;
    protected readonly DbSet<T> DbSet;

    public RepositoryBase(CareerSyncDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public IReadOnlyList<T> GetAll() => DbSet.AsNoTracking().ToList();

    public T? GetById(int id) => DbSet.Find(id);

    public T Add(T entity)
    {
        DbSet.Add(entity);
        Context.SaveChanges();
        return entity;
    }

    public bool Update(T entity)
    {
        // GetAll() returns untracked copies while GetById() returns a tracked
        // instance. If both were used for the same row in one request,
        // DbSet.Update(copy) would throw "another instance with the same key
        // is already being tracked". Copy the values onto the tracked
        // instance instead in that case.
        var tracked = DbSet.Local.FirstOrDefault(e => e.Id == entity.Id);
        if (tracked != null && !ReferenceEquals(tracked, entity))
            Context.Entry(tracked).CurrentValues.SetValues(entity);
        else
            DbSet.Update(entity);

        Context.SaveChanges();
        return true;
    }

    public bool Delete(int id)
    {
        var entity = DbSet.Find(id);
        if (entity is null) return false;
        DbSet.Remove(entity);
        Context.SaveChanges();
        return true;
    }
}
