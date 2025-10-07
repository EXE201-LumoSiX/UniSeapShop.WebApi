using Net.payOS;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Application.Interfaces.Commons;
using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly PayOS _payOs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggerService _loggerService;
    
    public PaymentService(PayOS payOs, IUnitOfWork unitOfWork, ILoggerService loggerService)
    {
        _payOs = payOs;
        _unitOfWork = unitOfWork;
        _loggerService = loggerService;
    }
    
}