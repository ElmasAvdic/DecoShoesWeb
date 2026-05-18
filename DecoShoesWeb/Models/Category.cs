using System.ComponentModel.DataAnnotations;

namespace DecoShoesWeb.Models
{
    public class Category
    {
        public int CategoryID { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public string? Description { get; set; }

        public int? ParentCategoryID { get; set; }

        public Category? ParentCategory { get; set; }

        public ICollection<Category> Subcategories { get; set; } = new List<Category>();

        public string? Slug { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
