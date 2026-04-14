using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Model;
using WebApplication1.DTOs;
namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
     public class CategoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================
        // 1. GET ALL
        // =========================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.Categories
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description
                })
                .ToListAsync();

            return Ok(data);
        }

        // =========================================
        // 2. GET BY ID
        // =========================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var c = await _context.Categories.FindAsync(id);

            if (c == null)
                return NotFound();

            var result = new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            };

            return Ok(result);
        }

        // =========================================
        // 3. CREATE
        // =========================================
        [HttpPost]
        public async Task<IActionResult> Create(CategoryDto dto)
        {
            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            dto.Id = category.Id;

            return Ok(dto);
        }

        // =========================================
        // 4. UPDATE
        // =========================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CategoryDto dto)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
                return NotFound();

            category.Name = dto.Name;
            category.Description = dto.Description;

            await _context.SaveChangesAsync();

            return Ok(dto);
        }

        // =========================================
        // 5. DELETE
        // =========================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
                return NotFound();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok("Deleted successfully");
        }
    }
}