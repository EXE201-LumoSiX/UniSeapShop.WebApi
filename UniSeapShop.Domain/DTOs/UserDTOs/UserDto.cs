using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniSeapShop.Domain.Enums;

namespace UniSeapShop.Domain.DTOs.UserDTOs
{
    public class UserDto
    {
        public Guid UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public RoleType? RoleName { get; set; }
        public Guid? SupplierId { get; set; }
    }
}
