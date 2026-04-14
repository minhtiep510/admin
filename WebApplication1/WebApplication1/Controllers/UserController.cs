using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.DTOs;
using WebApplication1.Model;

namespace WebApplication1.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================
        // GET ALL
        // =========================================
         [HttpGet]
       public async Task<IActionResult> GetAll()
        {
            var users = await _context.Users
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Address = u.Address,
                    Role = u.Role
                })
                .ToListAsync();

            return Ok(users);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound();

            var result = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Role = user.Role
            };

            return Ok(result);
         }
            [HttpPost]
         public async Task<IActionResult> Create(UserDto dto)
         {
             var user = new User
             {
                 FullName = dto.FullName,
                 Email = dto.Email,
                 PhoneNumber = dto.PhoneNumber,
                 Address = dto.Address,
                 Role = dto.Role,
                 PasswordHash = "hashed_password" // In real app, hash the password properly
             };

             _context.Users.Add(user);
             await _context.SaveChangesAsync();

             return CreatedAtAction(nameof(GetById), new { id = user.Id }, dto);
         }
            [HttpPut("{id}")]
         public async Task<IActionResult> Update(int id, UserDto dto)
         {
             var user = await _context.Users.FindAsync(id);

             if (user == null)
                 return NotFound();

             user.FullName = dto.FullName;
             user.Email = dto.Email;
             user.PhoneNumber = dto.PhoneNumber;
             user.Address = dto.Address;

             await _context.SaveChangesAsync();

             return NoContent();
          }
            [HttpDelete("{id}")]
          public async Task<IActionResult> Delete(int id)
          {
              var user = await _context.Users.FindAsync(id);

              if (user == null)
                  return NotFound();

              _context.Users.Remove(user);
              await _context.SaveChangesAsync();

              return NoContent();
           }
    }    
}