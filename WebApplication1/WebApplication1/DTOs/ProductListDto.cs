namespace WebApplication1.DTOs
{
    public class ProductListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; } 
        public string CategoryName { get; set; }
        public decimal StartingPrice { get; set; } 
        public string ThumbnailUrl { get; set; }
    }
}
