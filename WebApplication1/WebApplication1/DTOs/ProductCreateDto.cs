using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTOs
{
    public class ProductCreateDto
    {
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự")]
        public string Name { get; set; }
        public string Description { get; set; }
        [Required(ErrorMessage = "Danh mục là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Danh mục không hợp lệ")]
        public int CategoryId { get; set; }
    }
}
