using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Không tìm thấy file ảnh hợp lệ." });
            }

            try
            {
                // 1. Lưu file vào thư mục wwwroot/images của Backend
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // 2. Đổi tên file để tránh trùng lặp
                var fileExtension = Path.GetExtension(file.FileName);
                var newFileName = Guid.NewGuid().ToString() + fileExtension;
                var filePath = Path.Combine(folderPath, newFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 3. Trả về URL đường dẫn ảnh
                // KHÔNG DÙNG Request.Host vì nó sẽ trả về "localhost" mà điện thoại không hiểu.
                // Thay vào đó, hãy dùng IP mạng LAN của máy tính đang chạy backend.
                // IP này phải giống với IP bạn đã cấu hình trong file api.js của React Native.
                var baseUrl = "http://192.168.2.174:5136";
                var imageUrl = $"{baseUrl}/images/{newFileName}";

                return Ok(new { url = imageUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lưu file: " + ex.Message });
            }
        }

        // ==========================================
        // API MỚI: UPLOAD NHIỀU ẢNH CÙNG LÚC
        // ==========================================
        [HttpPost("multiple")]
        public async Task<IActionResult> UploadMultipleImages(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { message = "Không tìm thấy file ảnh hợp lệ." });
            }

            try
            {
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                var baseUrl = "http://192.168.2.174:5136"; // Thay bằng IP của bạn
                var uploadedUrls = new List<string>();

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        var fileExtension = Path.GetExtension(file.FileName);
                        var newFileName = Guid.NewGuid().ToString() + fileExtension;
                        var filePath = Path.Combine(folderPath, newFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        uploadedUrls.Add($"{baseUrl}/images/{newFileName}");
                    }
                }
                return Ok(new { urls = uploadedUrls });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lưu nhiều file: " + ex.Message });
            }
        }
    }
}