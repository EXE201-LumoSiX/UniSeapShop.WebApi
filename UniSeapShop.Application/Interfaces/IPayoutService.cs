using UniSeapShop.Domain.DTOs.PayoutDTOs;

namespace UniSeapShop.Application.Interfaces
{
    public interface IPayoutService
    {
        Task<PayoutDetailsDto> CreatePayout(Guid OrderId);
        Task<List<PayoutDetailsDto>> GetAllPayout();
        Task<PayoutDetailsDto> GetPayoutById(Guid payoutId);
        Task<PayoutDetailsDto> UpdatePayout(Guid payoutId, string status);
    }
}
