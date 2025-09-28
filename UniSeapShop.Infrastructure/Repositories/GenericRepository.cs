using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using UniSeapShop.Domain;
using UniSeapShop.Domain.Entities;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Infrastructure.Repositories
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseEntity
    {
        private readonly UniSeapShopDBContext _dbContext;
        private readonly DbSet<TEntity> _dbSet;
        public GenericRepository(UniSeapShopDBContext dBContext, DbSet<TEntity> dbSet)
        {
            _dbContext = dBContext;
            _dbSet = _dbContext.Set<TEntity>();
            _dbSet = dbSet;
        }
        public async Task<bool> Update(TEntity entity)
        {
            _dbSet.Update(entity);
            //   await _dbContext.SaveChangesAsync();
            return true;
        }
        public async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate = null,
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
    }
}
