namespace DecoShoesWeb.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public int? ProductSizeId { get; set; }

        public string ProductName { get; set; } = "";
        public string? ImageUrl { get; set; }

        public string? Size { get; set; }
        public int Quantity { get; set; }

        public decimal Price { get; set; }

        public decimal Total => Price * Quantity;
    }
}
