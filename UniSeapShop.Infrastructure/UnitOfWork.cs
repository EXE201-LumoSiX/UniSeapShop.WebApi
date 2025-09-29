using Microsoft.EntityFrameworkCore.Storage;
using UniSeapShop.Domain;
using UniSeapShop.Domain.Entities;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly UniSeapShopDBContext _context;
    private IDbContextTransaction? _transaction;
    private readonly IGenericRepository<User> _userRepository;
    private readonly IGenericRepository<Role> _roleRepository;

    public UnitOfWork(
        UniSeapShopDBContext context,
        IGenericRepository<User> userRepository,
        IGenericRepository<Role> roleRepository
    )
    {
        _context = context;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public IGenericRepository<User> Users => _userRepository;
    public IGenericRepository<Role> Roles => _roleRepository;

    // Transaction support
    public async Task BeginTransactionAsync()
    {
        if (_transaction != null) return;
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        try
        {
            if (_transaction != null)
            {
                await _context.SaveChangesAsync();
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