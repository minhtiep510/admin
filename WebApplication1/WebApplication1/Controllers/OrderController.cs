using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Model;
using WebApplication1.DTOs;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================
        // GET ALL
        // =========================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    o.Id,
                    CustomerName = o.User.FullName,
                    Phone = o.User.PhoneNumber,
                    Date = o.OrderDate,
                    o.TotalAmount,
                    Status = o.Status.ToLower()
                })
                .ToListAsync();

            return Ok(orders);
        }

        // =========================================
        // GET DETAIL (FULL)
        // =========================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Images)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            var result = new OrderItemResponseDto
            {
                Id = order.Id,
                CustomerName = order.User.FullName,
                Phone = order.User.PhoneNumber,
                ShippingAddress = order.ShippingAddress,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToLower(),

                Items = order.OrderDetails.Select(d => new OrderDetailDto
                {
                    ProductVariantId = d.ProductVariantId,
                    ProductName = d.ProductVariant.Product.Name,

                    Color = d.ProductVariant.Color,
                    Capacity = d.ProductVariant.Capacity,

                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,

                    ImageUrl = d.ProductVariant.Images
                        .Where(i => i.IsMain)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                }).ToList()
            };

            return Ok(result);
        }

        // =========================================
        // UPDATE STATUS
        // =========================================
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            order.Status = status;
            await _context.SaveChangesAsync();

            return Ok(order);
        }

        // =========================================
        // DELETE
        // =========================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            _context.OrderDetails.RemoveRange(order.OrderDetails);
            _context.Orders.Remove(order);

            await _context.SaveChangesAsync();

            return Ok("Deleted");
        }
    }
}