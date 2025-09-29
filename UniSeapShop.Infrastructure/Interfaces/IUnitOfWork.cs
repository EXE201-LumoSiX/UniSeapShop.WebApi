using UniSeapShop.Domain.Entities;

namespace UniSeapShop.Infrastructure.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }
    Task<int> SaveChangesAsync();
}