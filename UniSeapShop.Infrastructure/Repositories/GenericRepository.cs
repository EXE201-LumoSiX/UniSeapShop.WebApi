using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using UniSeapShop.Domain;
using UniSeapShop.Domain.Entities;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Infrastructure.Repositories;

public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseEntity
{
    private readonly UniSeapShopDBContext _dbContext;
    private readonly DbSet<TEntity> _dbSet;

    public GenericRepository(UniSeapShopDBContext dBContext)
    {
        _dbContext = dBContext;
        _dbSet = _dbContext.Set<TEntity>();
    }

    public Task<bool> Update(TEntity entity)
    {
        _dbSet.Update(entity);
        return Task.FromResult(true);
    }

    public async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> query = _dbSet;

        // Include các navigation properties
        foreach (var include in includes) query = query.Include(include);

        // Áp dụng predicate (nếu có)
        if (predicate != null) query = query.Where(predicate);

        // Lấy bản ghi đầu tiên
        return await query.FirstOrDefaultAsync();
    }

    public async Task<TEntity> AddAsync(TEntity entity)
    {
        //var currentUserId = _claimsService.CurrentUserId;

        //// Log để debug

        //// Chuyển tất cả các trường DateTime thành UTC
        //entity.CreatedAt = _timeService.GetCurrentTime().ToUniversalTime();
        //entity.UpdatedAt = _timeService.GetCurrentTime().ToUniversalTime();

        //if (entity.CreatedBy == Guid.Empty) entity.CreatedBy = currentUserId;

        //entity.UpdatedBy = currentUserId;

        var result = await _dbSet.AddAsync(entity);
        return result.Entity;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _dbContext.SaveChangesAsync();
    }
}