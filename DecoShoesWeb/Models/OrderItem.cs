namespace DecoShoesWeb.Models
{
    public class OrderItem
    {
        public int OrderItemID { get; set; }

        public int OrderID { get; set; }

        public Order? Order { get; set; }

        public int ProductID { get; set; }

        public Product? Product { get; set; }

        public int? ProductSizeID { get; set; }

        public ProductSize? ProductSize { get; set; }

        public string? Size { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }
    }
}