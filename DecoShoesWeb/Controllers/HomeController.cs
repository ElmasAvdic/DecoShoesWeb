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

        public async Task<IActionResult> Index(
            int? categoryId,
            string? filter,
            string? search,
            string? size,
            string? color,
            decimal? minPrice,
            decimal? maxPrice,
            string? sort)
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
                .ThenInclude(c => c.ParentCategory)
                .Include(p => p.ProductSizes);

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

            var filterBaseProducts = products;

            var availableColors = await filterBaseProducts
                .Where(p => p.Color != null && p.Color != "")
                .Select(p => p.Color!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var availableSizes = await filterBaseProducts
                .SelectMany(p => p.ProductSizes)
                .Where(s => s.StockQuantity > 0 && s.Size != null && s.Size != "")
                .Select(s => s.Size)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            size = string.IsNullOrWhiteSpace(size) ? null : size.Trim();
            color = string.IsNullOrWhiteSpace(color) ? null : color.Trim();
            sort = string.IsNullOrWhiteSpace(sort) ? null : sort.Trim().ToLowerInvariant();

            if (size != null)
            {
                products = products.Where(p => p.ProductSizes.Any(s => s.StockQuantity > 0 && s.Size == size));
            }

            if (color != null)
            {
                products = products.Where(p => p.Color != null && p.Color == color);
            }

            if (minPrice.HasValue)
            {
                products = products.Where(p =>
                    (p.DiscountPercent.HasValue && p.DiscountPercent > 0
                        ? p.Price * (1 - p.DiscountPercent.Value / 100)
                        : p.Price) >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                products = products.Where(p =>
                    (p.DiscountPercent.HasValue && p.DiscountPercent > 0
                        ? p.Price * (1 - p.DiscountPercent.Value / 100)
                        : p.Price) <= maxPrice.Value);
            }

            var orderedProducts = sort switch
            {
                "price-desc" => products
                    .OrderByDescending(p => p.DiscountPercent.HasValue && p.DiscountPercent > 0
                        ? p.Price * (1 - p.DiscountPercent.Value / 100)
                        : p.Price)
                    .ThenBy(p => p.Name),
                "newest" => products.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Name),
                "discount" => products.OrderByDescending(p => p.DiscountPercent ?? 0).ThenBy(p => p.Name),
                "price-asc" => products
                    .OrderBy(p => p.DiscountPercent.HasValue && p.DiscountPercent > 0
                        ? p.Price * (1 - p.DiscountPercent.Value / 100)
                        : p.Price)
                    .ThenBy(p => p.Name),
                _ => filter == "new"
                    ? products.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Name)
                    : products.OrderBy(p => p.ProductID)
            };

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
            ViewData["CurrentSize"] = size;
            ViewData["CurrentColor"] = color;
            ViewData["CurrentMinPrice"] = minPrice;
            ViewData["CurrentMaxPrice"] = maxPrice;
            ViewData["CurrentSort"] = sort;
            ViewData["AvailableColors"] = availableColors;
            ViewData["AvailableSizes"] = availableSizes;

            var productList = await orderedProducts
                .AsSplitQuery()
                .ToListAsync();

            productList = productList
                .DistinctBy(p => p.ProductID)
                .ToList();

            if (sort == null && filter != "new")
            {
                productList = productList
                    .OrderBy(_ => Random.Shared.Next())
                    .ToList();
            }

            return View(productList);
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
