using UniSeapShop.Domain;
using UniSeapShop.Domain.Entities;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Infrastructure;

public class UnitOfWork
{
    private readonly UniSeapShopDBContext _context;

    public UnitOfWork(
        UniSeapShopDBContext context,
        IGenericRepository<User> userRepository,
        IGenericRepository<Role> roleRepository
    )
    {
        _context = context;
        Users = userRepository;
        Roles = roleRepository;
    }


    public IGenericRepository<User> Users { get; }

    public IGenericRepository<Role> Roles { get; }
}