using UniSeapShop.Domain.Entities;

namespace UniSeapShop.Infrastructure.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }
    IGenericRepository<Role> Roles { get; }
    Task<int> SaveChangesAsync();
}