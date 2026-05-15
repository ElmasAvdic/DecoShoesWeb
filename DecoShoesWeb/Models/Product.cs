using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DecoShoesWeb.Models
{
    public class Product
    {
        public int ProductID { get; set; }

        [Display(Name = "Category")]
        public int CategoryID { get; set; }

        public Category? Category { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Brand { get; set; }

        public string? Color { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public int? StockQuantity { get; set; }

        public string? ImageUrl { get; set; }

        public string? Description { get; set; }

        public bool HasSizes { get; set; }

        public decimal? DiscountPercent { get; set; }

        public ICollection<ProductSize> ProductSizes { get; set; } = new List<ProductSize>();
    }
}