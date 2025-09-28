using UniSeapShop.Domain.Entities;

namespace UniSeapShop.Infrastructure.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync();
        IGenericRepository<User> Users { get; }
    }
}
