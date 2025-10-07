using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;
using UniSeapShop.Domain;
using UniSeapShop.Domain.Entities;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly UniSeapShopDBContext _dbContext;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(
        UniSeapShopDBContext dbContext, IGenericRepository<User> users,
        IGenericRepository<Customer> customers,
        IGenericRepository<Order> orders,
        IGenericRepository<OrderDetail> ordersDetail,
        IGenericRepository<Product> products,
        IGenericRepository<Category> categories,
        IGenericRepository<Supplier> suppliers,
        IGenericRepository<Role> roles,
        IGenericRepository<OtpVerification> otpVerifications
    )
    {
        _dbContext = dbContext;
        Users = users;
        Orders = orders;
        OrdersDetail = ordersDetail;
        Products = products;
        Suppliers = suppliers;
        Roles = roles;
        OtpVerifications = otpVerifications;
        Customers = customers;
        Categories = categories;
    }

    public IGenericRepository<User> Users { get; }
    public IGenericRepository<Role> Roles { get; }
    public IGenericRepository<Order> Orders { get; }
    public IGenericRepository<OrderDetail> OrdersDetail { get; }
    public IGenericRepository<Product> Products { get; }
    public IGenericRepository<Category> Categories { get; }
    public IGenericRepository<Supplier> Suppliers { get; }
    public IGenericRepository<Customer> Customers { get; }
    public IGenericRepository<OtpVerification> OtpVerifications { get; }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _dbContext.SaveChangesAsync();
    }

    // Where
    public IQueryable<T> Where<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        return _dbContext.Set<T>().Where(predicate);
    }

    // Select
    public IQueryable<TResult> Select<T, TResult>(Expression<Func<T, TResult>> selector) where T : class
    {
        return _dbContext.Set<T>().Select(selector);
    }

    // Transaction support
    public async Task BeginTransactionAsync()
    {
        if (_transaction != null) return;
        _transaction = await _dbContext.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        try
        {
            if (_transaction != null)
            {
                await _dbContext.SaveChangesAsync();
                await _transaction.CommitAsync();
            }
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackAsync()
    {
        try
        {
            if (_transaction != null)
                await _transaction.RollbackAsync();
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}