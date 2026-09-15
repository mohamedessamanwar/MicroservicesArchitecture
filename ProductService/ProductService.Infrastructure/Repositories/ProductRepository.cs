using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using ProductService.Infrastructure.Data;

namespace ProductService.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<bool> DecreaseStockAsync(Guid id, int amount, CancellationToken cancellationToken = default)
    {
        var updatedRows = await _context.Products
            .Where(p => p.Id == id && p.StockCount >= amount)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.StockCount, p => p.StockCount - amount), 
                cancellationToken);
        return updatedRows > 0;
    }

    public async Task<bool> IncreaseStockAsync(Guid id, int amount, CancellationToken cancellationToken = default)
    {
        var updatedRows = await _context.Products
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.StockCount, p => p.StockCount + amount), 
                cancellationToken);
        return updatedRows > 0;
    }

    public async Task<bool> DecreaseStockBulkAsync(IEnumerable<(Guid ProductId, int Amount)> items, CancellationToken cancellationToken = default)
    {
        var itemDict = items.ToDictionary(i => i.ProductId, i => i.Amount);
        var productIds = itemDict.Keys.ToList();
        
        // Load entities so EF Core Change Tracker can enforce Optimistic Concurrency Control (OCC)
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Count)
        {
            return false; // Some products not found
        }

        foreach (var product in products)
        {
            var amountToDecrease = itemDict[product.Id];
            if (product.StockCount < amountToDecrease)
            {
                return false; // Insufficient stock
            }
            product.StockCount -= amountToDecrease;
        }

        try
        {
            // This relies on the [Timestamp]/uint Version property for OCC in EF Core
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // OCC conflict detected
            return false;
        }
    }

    public async Task<bool> IncreaseStockBulkAsync(IEnumerable<(Guid ProductId, int Amount)> items, CancellationToken cancellationToken = default)
    {
        var itemDict = items.ToDictionary(i => i.ProductId, i => i.Amount);
        var productIds = itemDict.Keys.ToList();
        
        // Load entities for OCC
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Count)
        {
            return false; // Some products not found
        }

        foreach (var product in products)
        {
            var amountToIncrease = itemDict[product.Id];
            product.StockCount += amountToIncrease;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // OCC conflict detected
            return false;
        }
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<Product>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> FindAsync(Func<Product, bool> predicate, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_context.Products.Where(predicate).ToList());
    }

    public async Task<Product> AddAsync(Product entity, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Product entity, CancellationToken cancellationToken = default)
    {
        _context.Products.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Product entity, CancellationToken cancellationToken = default)
    {
        _context.Products.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
