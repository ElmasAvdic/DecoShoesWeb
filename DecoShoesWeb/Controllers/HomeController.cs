using System.Diagnostics;
using DecoShoesWeb.Data;
using DecoShoesWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DecoShoesWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(int? categoryId, string? filter, string? search)
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var rootCategories = categories
                .Where(c => c.ParentCategoryID == null)
                .ToList();

            Category? selectedCategory = null;
            Category? selectedRootCategory = null;

            if (categoryId.HasValue)
            {
                selectedCategory = categories.FirstOrDefault(c => c.CategoryID == categoryId.Value);

                if (selectedCategory == null)
                {
                    categoryId = null;
                }
                else
                {
                    selectedRootCategory = selectedCategory.ParentCategoryID == null
                        ? selectedCategory
                        : rootCategories.FirstOrDefault(c => c.CategoryID == selectedCategory.ParentCategoryID);
                }
            }


            IQueryable<Product> products = _context.Products
                .Include(p => p.Category)
                .ThenInclude(c => c.ParentCategory);

            if (selectedCategory != null)
            {
                if (selectedCategory.ParentCategoryID == null)
                {
                    var categoryIds = categories
                        .Where(c => c.CategoryID == selectedCategory.CategoryID || c.ParentCategoryID == selectedCategory.CategoryID)
                        .Select(c => c.CategoryID)
                        .ToList();

                    products = products.Where(p => categoryIds.Contains(p.CategoryID));
                }
                else
                {
                    products = products.Where(p => p.CategoryID == selectedCategory.CategoryID);
                }
            }

            filter = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim().ToLowerInvariant();

            if (filter == "discounted")
            {
                products = products.Where(p => p.DiscountPercent.HasValue && p.DiscountPercent > 0);
            }

            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

            if (search != null)
            {
                products = products.Where(p =>
                    p.Name.Contains(search) ||
                    (p.Brand != null && p.Brand.Contains(search)) ||
                    (p.Color != null && p.Color.Contains(search)) ||
                    (p.Description != null && p.Description.Contains(search)) ||
                    (p.Category != null && p.Category.Name.Contains(search)) ||
                    (p.Category != null && p.Category.ParentCategory != null && p.Category.ParentCategory.Name.Contains(search)));
            }

            var orderedProducts = filter == "new"
                ? products.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Name)
                : products.OrderBy(p => p.Price).ThenBy(p => p.Name);

            ViewData["RootCategories"] = rootCategories;
            ViewData["SubcategoriesByParent"] = categories
                .Where(c => c.ParentCategoryID.HasValue)
                .GroupBy(c => c.ParentCategoryID!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList());
            ViewData["SelectedCategoryId"] = categoryId;
            ViewData["SelectedRootCategoryId"] = selectedRootCategory?.CategoryID;
            ViewData["SelectedCategory"] = selectedCategory;
            ViewData["CurrentFilter"] = filter;
            ViewData["CurrentSearch"] = search;

            return View(await orderedProducts.ToListAsync());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Location()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
