using System.Linq.Expressions;
using UniSeapShop.Domain.Entities;

namespace UniSeapShop.Infrastructure.Interfaces
{
    public interface IGenericRepository<TEntity> where TEntity : BaseEntity
    {
        Task<bool> Update(TEntity entity);
        Task<TEntity?> FirstOrDefaultAsync(
       Expression<Func<TEntity, bool>> predicate = null,
       params Expression<Func<TEntity, object>>[] includes);
    }
}
