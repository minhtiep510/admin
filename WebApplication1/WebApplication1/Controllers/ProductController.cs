using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.DTOs;
using WebApplication1.Model;

namespace WebApplication1.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================
        // 1. GET ALL (Danh sách sản phẩm)
        // =========================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Images)
                .Select(p => new ProductListDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    CategoryName = p.Category.Name,

                    StartingPrice = p.ProductVariants
                        .OrderBy(v => v.Price)
                        .Select(v => v.Price)
                        .FirstOrDefault(),

                    ThumbnailUrl = p.ProductVariants
                        .SelectMany(v => v.Images)
                        .Where(i => i.IsMain)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(products);
        }

        // =========================================
        // 2. GET DETAIL
        // =========================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Images)
                .Include(p => p.TechnicalSpecifications) // Include TechnicalSpecifications
                .AsNoTracking() // Thêm AsNoTracking nếu không cần theo dõi thay đổi
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            var result = new ProductDetailDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                CategoryName = product.Category.Name,

                Variants = product.ProductVariants.Select(v => new ProductVariantDto
                {
                    Id = v.Id,
                    SKU = v.SKU,
                    Price = v.Price,
                    StockQuantity = v.StockQuantity,
                    Color = v.Color,
                    Capacity = v.Capacity,

                    Images = v.Images.Select(i => new VariantImageDto
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl,
                        IsMain = i.IsMain
                    }).ToList()
                }).ToList(),

                Specifications = product.TechnicalSpecifications.Select(s => new TechnicalSpecDto // Map TechnicalSpecifications
                {
                    SpecName = s.SpecName,
                    SpecValue = s.SpecValue
                }).ToList()
            };

            return Ok(result);
        }

        // =========================================
        // 3. CREATE PRODUCT
        // =========================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState); // Trả về lỗi validation tự động
                }

                // Check if category exists
                var category = await _context.Categories.FindAsync(dto.CategoryId);
                if (category == null)
                    return BadRequest("Danh mục không tồn tại.");

                // Create product
                var product = new Product
                {
                    Name = dto.Name.Trim(),
                    Description = (dto.Description ?? "").Trim(),
                    CategoryId = dto.CategoryId,
                    CreatedAt = DateTime.Now
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    id = product.Id, 
                    name = product.Name, 
                    description = product.Description,
                    categoryId = product.CategoryId,
                    message = "Đã thêm sản phẩm thành công"
                });
            }
            catch (Exception) // Bắt lỗi chung
            {
                // Log lỗi chi tiết ở đây (ví dụ: _logger.LogError(ex, "Error creating product"));
                return StatusCode(500, "Đã xảy ra lỗi trong quá trình thêm sản phẩm."); // Thông báo lỗi chung cho client
            }
        }

        // =========================================
        // 4. UPDATE PRODUCT
        // =========================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductCreateDto dto)
        {
            try
            {
                // Check model state
                if (!ModelState.IsValid) // Sử dụng validation tự động từ Data Annotations
                {
                    return BadRequest(ModelState);
                }

                // Manual validation (có thể bỏ nếu đã dùng Data Annotations đầy đủ)
                // Tuy nhiên, kiểm tra CategoryId vẫn cần thiết vì nó là khóa ngoại
                // và Data Annotations không kiểm tra sự tồn tại của đối tượng liên quan.
                if (dto.CategoryId <= 0)
                    return BadRequest("Danh mục không hợp lệ");

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound("Sản phẩm không tồn tại");

                // Check if category exists
                var category = await _context.Categories.FindAsync(dto.CategoryId);
                if (category == null) // Kiểm tra sự tồn tại của danh mục
                    return BadRequest("Danh mục không tồn tại");

                product.Name = dto.Name.Trim();
                product.Description = dto.Description?.Trim() ?? "";
                product.CategoryId = dto.CategoryId;

                await _context.SaveChangesAsync();

                return Ok(new { 
                    id = product.Id, 
                    name = product.Name, 
                    description = product.Description,
                    categoryId = product.CategoryId,
                    message = "Cập nhật sản phẩm thành công"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, "Đã xảy ra lỗi trong quá trình cập nhật sản phẩm.");
            }
        }

        // =========================================
        // 5. DELETE PRODUCT
        // =========================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductVariants)
                .ThenInclude(v => v.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok("Xóa sản phẩm thành công.");
        }

        // =========================================
        // 6. ADD VARIANT IMAGES
        // =========================================
        [HttpPost("{productId}/variants")]
public async Task<IActionResult> CreateVariant(int productId, [FromBody] ProductVariantDto dto)
{
    var variant = new ProductVariant
    {
        ProductId = productId,
        SKU = dto.SKU ?? $"SKU-{DateTime.Now.Ticks}",
        Color = dto.Color,
        Capacity = dto.Capacity ?? "",
        Price = dto.Price,
        StockQuantity = dto.StockQuantity 
    };

    _context.ProductVariants.Add(variant);
    await _context.SaveChangesAsync();

    // 🔥 thêm ảnh
    if (dto.Images != null && dto.Images.Any())
    {
        bool isFirst = true;

        foreach (var img in dto.Images)
        {
            _context.Images.Add(new Image
            {
                ProductVariantId = variant.Id,
                ImageUrl = img.ImageUrl,
                IsMain = img.IsMain || isFirst
            });

            isFirst = false;
        }

        await _context.SaveChangesAsync();  
    }

    return Ok("Tạo variant + ảnh thành công");
}
[HttpPut("variant/{id}")]
public async Task<IActionResult> UpdateVariant(int id, [FromBody] ProductVariantDto dto)
{
    try
    {
        var variant = await _context.ProductVariants
            .Include(v => v.Images)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (variant == null)
            return NotFound("Biến thể không tồn tại");

        variant.SKU = dto.SKU ?? variant.SKU;
        variant.Color = dto.Color;
        variant.Capacity = dto.Capacity ?? "";
        variant.Price = dto.Price;
        variant.StockQuantity = dto.StockQuantity;

        // 🔥 XÓA ảnh cũ
        _context.Images.RemoveRange(variant.Images);

        // 🔥 THÊM lại ảnh mới
        if (dto.Images != null && dto.Images.Any())
        {
            bool isFirst = true;

            foreach (var img in dto.Images)
            {
                _context.Images.Add(new Image
                {
                    ProductVariantId = variant.Id,
                    ImageUrl = img.ImageUrl,
                    IsMain = img.IsMain || isFirst
                });

                isFirst = false;
            }
        }

        await _context.SaveChangesAsync();

        return Ok("Cập nhật variant + ảnh thành công");
    }
    catch
    {
        return StatusCode(500, "Lỗi update variant");
    }
}
[HttpDelete("variant/{id}")]
public async Task<IActionResult> DeleteVariant(int id)
{
    var variant = await _context.ProductVariants
        .Include(v => v.Images)
        .FirstOrDefaultAsync(v => v.Id == id);

    if (variant == null)
        return NotFound();

    _context.Images.RemoveRange(variant.Images);
    _context.ProductVariants.Remove(variant);

    await _context.SaveChangesAsync();

    return Ok("Đã xóa variant + ảnh");
}
    }
}
