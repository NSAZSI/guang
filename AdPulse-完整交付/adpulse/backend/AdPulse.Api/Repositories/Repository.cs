using AdPulse.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AdPulse.Api.Repositories;

/// <summary>通用仓储：简单实体的标准数据访问。</summary>
public interface IRepository<T> where T : class
{
    IQueryable<T> Query();
    Task<T?> GetAsync(int id);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveAsync();
}

public class Repository<T>(AppDbContext db) : IRepository<T> where T : class
{
    protected readonly AppDbContext Db = db;
    public IQueryable<T> Query() => Db.Set<T>().AsQueryable();
    public Task<T?> GetAsync(int id) => Db.Set<T>().FindAsync(id).AsTask();
    public async Task AddAsync(T entity) => await Db.Set<T>().AddAsync(entity);
    public void Update(T entity) => Db.Set<T>().Update(entity);
    public void Remove(T entity) => Db.Set<T>().Remove(entity);
    public Task<int> SaveAsync() => Db.SaveChangesAsync();
}