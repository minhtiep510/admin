using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.DTOs;
using WebApplication1.Model;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(AppDbContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("total")]
        public async Task<IActionResult> GetUserCount()
        {
            try
            {
                var count = await _context.Users.CountAsync();
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error counting users: {ex.Message}");
                return StatusCode(500, new { message = "Error counting users", error = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                _logger.LogInformation("Fetching all users");

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

                _logger.LogInformation($"Retrieved {users.Count} users");
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching users: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching users", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy người dùng theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                _logger.LogInformation($"Fetching user with ID: {id}");

                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    _logger.LogWarning($"User with ID {id} not found");
                    return NotFound(new { message = "User not found" });
                }

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
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching user {id}: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching user", error = ex.Message });
            }
        }

        /// <summary>
        /// Tạo người dùng mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserDto dto)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(dto.FullName))
                    return BadRequest(new { message = "Họ tên không được trống" });

                if (string.IsNullOrWhiteSpace(dto.Email))
                    return BadRequest(new { message = "Email không được trống" });

                // Validate email format
                var emailValidator = new EmailAddressAttribute();
                if (!emailValidator.IsValid(dto.Email))
                    return BadRequest(new { message = "Email không hợp lệ" });

                // Check email unique
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
                if (existingUser != null)
                    return BadRequest(new { message = "Email đã tồn tại trong hệ thống" });

                // Validate phone if provided
                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(dto.PhoneNumber, @"^\d{9,15}$"))
                        return BadRequest(new { message = "Số điện thoại không hợp lệ" });
                }

                _logger.LogInformation($"Creating new user: {dto.Email}");

                var user = new User
                {
                    FullName = dto.FullName.Trim(),
                    Email = dto.Email.Trim().ToLower(),
                    PhoneNumber = dto.PhoneNumber?.Trim(),
                    Address = dto.Address?.Trim(),
                    Role = string.IsNullOrWhiteSpace(dto.Role) ? "customer" : dto.Role.ToLower(),
                    PasswordHash = "default_hash" // In production, hash password properly
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User created successfully with ID: {user.Id}");

                var responseDto = new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    Role = user.Role
                };

                return CreatedAtAction(nameof(GetById), new { id = user.Id }, responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating user: {ex.Message}");
                return StatusCode(500, new { message = "Error creating user", error = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật người dùng
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserDto dto)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(dto.FullName))
                    return BadRequest(new { message = "Họ tên không được trống" });

                if (string.IsNullOrWhiteSpace(dto.Email))
                    return BadRequest(new { message = "Email không được trống" });

                // Validate email format
                var emailValidator = new EmailAddressAttribute();
                if (!emailValidator.IsValid(dto.Email))
                    return BadRequest(new { message = "Email không hợp lệ" });

                // Validate phone if provided
                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(dto.PhoneNumber, @"^\d{9,15}$"))
                        return BadRequest(new { message = "Số điện thoại không hợp lệ" });
                }

                _logger.LogInformation($"Updating user with ID: {id}");

                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    _logger.LogWarning($"User with ID {id} not found for update");
                    return NotFound(new { message = "User not found" });
                }

                // Check email unique (excluding current user)
                var emailExists = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower() && u.Id != id);
                if (emailExists != null)
                    return BadRequest(new { message = "Email đã được sử dụng bởi người dùng khác" });

                // Update properties
                user.FullName = dto.FullName.Trim();
                user.Email = dto.Email.Trim().ToLower();
                user.PhoneNumber = dto.PhoneNumber?.Trim();
                user.Address = dto.Address?.Trim();
                if (!string.IsNullOrWhiteSpace(dto.Role))
                    user.Role = dto.Role.ToLower();

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {id} updated successfully");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating user {id}: {ex.Message}");
                return StatusCode(500, new { message = "Error updating user", error = ex.Message });
            }
        }

        /// <summary>
        /// Xóa người dùng
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation($"Deleting user with ID: {id}");

                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    _logger.LogWarning($"User with ID {id} not found for deletion");
                    return NotFound(new { message = "User not found" });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {id} deleted successfully");

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError($"Error deleting user {id} - Foreign key constraint: {ex.Message}");
                return BadRequest(new { message = "Cannot delete user: User has related orders" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting user {id}: {ex.Message}");
                return StatusCode(500, new { message = "Error deleting user", error = ex.Message });
            }
        }
    }
}