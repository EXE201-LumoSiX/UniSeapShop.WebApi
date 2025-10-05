using System.Linq.Expressions;
using UniSeapShop.Domain.Entities;

namespace UniSeapShop.Infrastructure.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }
    IGenericRepository<Customer> Customers { get; }
    IGenericRepository<Order> Orders { get; }
    IGenericRepository<OrderDetail> OrdersDetail { get; }
    IGenericRepository<Product> Products { get; }
    IGenericRepository<Category> Categories { get; }
    IGenericRepository<Supplier> Suppliers { get; }
    IGenericRepository<Role> Roles { get; }
    IGenericRepository<OtpVerification> OtpVerifications { get; }
    IGenericRepository<Category> Categories { get; }
    Task<int> SaveChangesAsync();
    IQueryable<T> Where<T>(Expression<Func<T, bool>> predicate) where T : class;
    IQueryable<TResult> Select<T, TResult>(Expression<Func<T, TResult>> selector) where T : class;
}