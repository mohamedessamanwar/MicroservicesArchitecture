using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Data;

namespace Payment.Infrastructure.Repositories;

public class EfGenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly AppDbContext _context;

    public EfGenericRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Set<T>().AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entry.Entity;
    }

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _context.Set<T>().Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<T>().FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<T>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<T>().ToListAsync(cancellationToken);
    }
}