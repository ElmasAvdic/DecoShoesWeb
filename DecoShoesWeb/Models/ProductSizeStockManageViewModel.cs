using System.ComponentModel.DataAnnotations;

namespace DecoShoesWeb.Models
{
    public class ProductSizeStockManageViewModel
    {
        public int ProductID { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public List<ProductSizeStockRowViewModel> ExistingSizes { get; set; } = new();

        public List<ProductSizeStockRowViewModel> NewSizes { get; set; } = new();
    }

    public class ProductSizeStockRowViewModel
    {
        public int? ProductSizeID { get; set; }

        [Display(Name = "Broj")]
        public string? Size { get; set; }

        [Display(Name = "Količina")]
        [Range(0, int.MaxValue, ErrorMessage = "Količina ne može biti manja od 0.")]
        public int StockQuantity { get; set; }

        public bool Delete { get; set; }
    }
}
