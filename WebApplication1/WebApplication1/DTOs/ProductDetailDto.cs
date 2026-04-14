namespace WebApplication1.DTOs
{
    public class ProductDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CategoryName { get; set; }

        public List<ProductVariantDto> Variants { get; set; }
        public List<TechnicalSpecDto> Specifications { get; set; }

        public List<ProductImageDto> Images { get; set; }
    }
}
