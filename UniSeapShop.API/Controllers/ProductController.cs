using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSeapShop.Application.Interfaces;
using UniSeapShop.Domain.DTOs.ProductDTOs;

namespace UniSeapShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductController(IProductService productService)
        {
            _productService = productService;
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto createProductDto)
        {
            var product = await _productService.CreateProductAsunc(createProductDto);
            return CreatedAtAction(nameof(GetProductById), new { productId = product.Id }, product);
        }
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _productService.GetAllProductsAsync();
            return Ok(products);
        }
        [HttpGet("{productId}")]
        public async Task<IActionResult> GetProductById(Guid productId)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }
        [HttpPut("{productId}")]
        [Authorize]
        public async Task<IActionResult> UpdateProduct(Guid productId, [FromBody] UpdateProductDto updateProductDto)
        {
            var updatedProduct = await _productService.UpdateProductAsync(productId, updateProductDto);
            if (updatedProduct == null)
            {
                return NotFound();
            }
            return Ok(updatedProduct);
        }
        [HttpDelete("{productId}")]
        [Authorize]
        public async Task<IActionResult> DeleteProduct(Guid productId)
        {
            var result = await _productService.DeleteProductAsync(productId);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
