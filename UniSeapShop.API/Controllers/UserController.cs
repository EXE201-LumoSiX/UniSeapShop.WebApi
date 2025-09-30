using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniSeapShop.API.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        [HttpGet("profile")]
        [Authorize]
        public IActionResult GetProfile()
        {
            // Trả về thông tin user hiện tại (demo)
            return Ok(new
            {
                Message = "Bạn đã đăng nhập thành công và có thể truy cập API này!",
                User = User.Identity?.Name ?? "Unknown"
            });
        }

        [HttpGet("admin")]
        [Authorize(Policy = "AdminPolicy")]
        public IActionResult GetAdmin()
        {
            return Ok(new
            {
                Message = "Bạn có quyền Admin!",
                User = User.Identity?.Name ?? "Unknown"
            });
        }

        [HttpGet("supplier")]
        [Authorize(Policy = "SupplierPolicy")]
        public IActionResult GetSupplier()
        {
            return Ok(new
            {
                Message = "Bạn có quyền Supplier!",
                User = User.Identity?.Name ?? "Unknown"
            });
        }

        [HttpGet("customer")]
        [Authorize(Policy = "CustomerPolicy")]
        public IActionResult GetCustomer()
        {
            return Ok(new
            {
                Message = "Bạn có quyền Customer!",
                User = User.Identity?.Name ?? "Unknown"
            });
        }
    }
}

