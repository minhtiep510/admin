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
     // Đường dẫn gọi API: GET /api/product/total-stock
    [HttpGet("total-stock")]
    public async Task<IActionResult> GetTotalStock()
    {
        var totalStock = await _context.ProductVariants.SumAsync(v => v.StockQuantity);
        return Ok(new { totalStock });
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
                    CategoryId = p.CategoryId,
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

            var result = new 
            {
                id = product.Id,
                name = product.Name,
                description = product.Description,
                categoryId = product.CategoryId, // Trả về thêm ID danh mục để App chọn sẵn
                categoryName = product.Category?.Name,

                variants = product.ProductVariants.Select(v => new 
                {
                    id = v.Id,
                    sku = v.SKU,
                    price = v.Price,
                    stockQuantity = v.StockQuantity,
                    color = v.Color,
                    capacity = v.Capacity,

                    images = v.Images.Select(i => new 
                    {
                        id = i.Id,
                        imageUrl = i.ImageUrl,
                        isMain = i.IsMain
                    }).ToList()
                }).ToList(),

                specifications = product.TechnicalSpecifications.Select(s => new 
                {
                    id = s.Id,
                    key = s.SpecName,     // App cần tên biến là "key"
                    value = s.SpecValue   // App cần tên biến là "value"
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
            // 1. Tải sản phẩm và tất cả dữ liệu con liên quan
            var product = await _context.Products
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Images)
                .Include(p => p.TechnicalSpecifications)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound("Sản phẩm không tồn tại.");

            // 2. Kiểm tra xem sản phẩm có trong đơn hàng nào không
            var variantIds = product.ProductVariants.Select(v => v.Id).ToList();
            var isProductInAnyOrder = await _context.OrderDetails
                .AnyAsync(od => variantIds.Contains(od.ProductVariantId));

            if (isProductInAnyOrder)
            {
                return BadRequest("Không thể xóa sản phẩm này vì đã có trong một hoặc nhiều đơn hàng đã đặt.");
            }

            // 3. Nếu không có ràng buộc, tiến hành xóa
            try
            {
                var imagesToDelete = product.ProductVariants.SelectMany(v => v.Images).ToList();

                // Xóa các bản ghi trong DB theo thứ tự: con -> cha
                if (imagesToDelete.Any()) _context.Images.RemoveRange(imagesToDelete);
                if (product.TechnicalSpecifications.Any()) _context.TechnicalSpecifications.RemoveRange(product.TechnicalSpecifications);
                if (product.ProductVariants.Any()) _context.ProductVariants.RemoveRange(product.ProductVariants);
                _context.Products.Remove(product);

                await _context.SaveChangesAsync(); // Lưu thay đổi vào DB

                // 4. Sau khi DB xóa thành công, tiến hành xóa file vật lý trên server
                foreach (var image in imagesToDelete)
                {
                    try
                    {
                        var fileName = Path.GetFileName(new Uri(image.ImageUrl).AbsolutePath);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", fileName);
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Ghi log lỗi xóa file nhưng không dừng tiến trình
                        Console.WriteLine($"Lỗi không thể xóa file {image.ImageUrl}: {ex.Message}");
                    }
                }

                return Ok("Xóa sản phẩm thành công.");
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, $"Lỗi cơ sở dữ liệu khi xóa sản phẩm: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // =========================================
        // 6. ADD VARIANT IMAGES
        // =========================================
        [HttpPost("{productId}/variants")]
        public async Task<IActionResult> CreateVariant(int productId, [FromBody] ProductVariantDto dto)
        {
            try
            {
                var variant = new ProductVariant
                {
                    ProductId = productId,
                    SKU = string.IsNullOrWhiteSpace(dto.SKU) ? $"SKU-{DateTime.Now.Ticks}" : dto.SKU,
                    Color = dto.Color ?? "Mặc định",
                    Capacity = dto.Capacity ?? "",
                    Price = dto.Price,
                    StockQuantity = dto.StockQuantity 
                };

                _context.ProductVariants.Add(variant);
                await _context.SaveChangesAsync();

                if (dto.Images != null && dto.Images.Any())
                {
                    bool isFirst = true;
                    foreach (var img in dto.Images)
                    {
                        if (!string.IsNullOrEmpty(img.ImageUrl))
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
                }

                return Ok(new { message = "Tạo variant + ảnh thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi Server: {ex.Message} - {ex.InnerException?.Message}" });
            }
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
                    return NotFound(new { message = "Biến thể không tồn tại" });

                variant.SKU = string.IsNullOrWhiteSpace(dto.SKU) ? variant.SKU : dto.SKU;
                variant.Color = dto.Color ?? variant.Color;
                variant.Capacity = dto.Capacity ?? "";
                variant.Price = dto.Price;
                variant.StockQuantity = dto.StockQuantity;

                _context.Images.RemoveRange(variant.Images);

                if (dto.Images != null && dto.Images.Any())
                {
                    bool isFirst = true;
                    foreach (var img in dto.Images)
                    {
                        if (!string.IsNullOrEmpty(img.ImageUrl))
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
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Cập nhật variant + ảnh thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi Server: {ex.Message} - {ex.InnerException?.Message}" });
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
