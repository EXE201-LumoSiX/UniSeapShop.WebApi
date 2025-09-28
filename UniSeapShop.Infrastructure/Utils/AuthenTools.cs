using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace UniSeapShop.Infrastructure.Utils
{
    public class AuthenTools
    {
        public static string? GetCurrentUserId(ClaimsIdentity? identity)
        {
            if (identity == null)
                return null;

            var userId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // Log userId value
            Console.WriteLine($"Extracted UserId from claims: {userId}");
            return userId;
        }
    }
}
