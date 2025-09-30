using UniSeapShop.Infrastructure.Interfaces;

namespace UniSeapShop.Infrastructure.Commons;

public class CurrentTime : ICurrentTime
{
    public DateTime GetCurrentTime()
    {
        return DateTime.UtcNow.ToUniversalTime();
    }
}