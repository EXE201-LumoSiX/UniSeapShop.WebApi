using UniSeapShop.Domain;
using UniSeapShop.Domain.Entities;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Infrastructure;

public class UnitOfWork 
{
    private readonly UniSeapShopDBContext _context;
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


    public IGenericRepository<User> Users => _userRepository;
    public IGenericRepository<Role> Roles => _roleRepository;

    
}