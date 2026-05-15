namespace DecoShoesWeb.Models
{
    public class ProductSize
    {
        public int ProductSizeID { get; set; }

        public int ProductID { get; set; }

        public Product? Product { get; set; }

        public string Size { get; set; }

        public int StockQuantity { get; set; }
    }
}