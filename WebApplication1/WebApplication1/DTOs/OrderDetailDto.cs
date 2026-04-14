namespace WebApplication1.DTOs
{
    public class OrderDetailDto
    {
     public int ProductVariantId { get; set; }

    public string ProductName { get; set; }

    public string Color { get; set; }
    public string Capacity { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public string ImageUrl { get; set; }
    }
}