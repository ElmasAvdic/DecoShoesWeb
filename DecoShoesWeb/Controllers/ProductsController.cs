using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DecoShoesWeb.Data;
using DecoShoesWeb.Models;

namespace DecoShoesWeb.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index(int? categoryId, string? search)
        {
            var products = _context.Products
                .Include(p => p.Category)
                .ThenInclude(c => c.ParentCategory)
                .Include(p => p.ProductSizes)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                products = products.Where(p =>
                    p.CategoryID == categoryId.Value ||
                    p.Category!.ParentCategoryID == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                products = products.Where(p =>
                    p.Name.Contains(term) ||
                    (p.Brand != null && p.Brand.Contains(term)) ||
                    (p.Color != null && p.Color.Contains(term)) ||
                    (p.Description != null && p.Description.Contains(term)) ||
                    (p.Category != null && p.Category.Name.Contains(term)) ||
                    (p.Category != null && p.Category.ParentCategory != null && p.Category.ParentCategory.Name.Contains(term)));
            }

            await LoadCategoryFilterOptionsAsync(categoryId);
            ViewData["CurrentSearch"] = search;

            return View(await products
                .OrderBy(p => p.Category!.ParentCategory != null ? p.Category.ParentCategory.DisplayOrder : p.Category.DisplayOrder)
                .ThenBy(p => p.Category!.ParentCategory != null ? p.Category.ParentCategory.Name : p.Category.Name)
                .ThenBy(p => p.Category!.DisplayOrder)
                .ThenBy(p => p.Name)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .ThenInclude(c => c.ParentCategory)
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(m => m.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            var similarProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.ProductID != product.ProductID && p.CategoryID == product.CategoryID)
                .OrderBy(p => p.Name)
                .Take(12)
                .ToListAsync();

            if (!similarProducts.Any() && product.Category?.ParentCategoryID != null)
            {
                var siblingCategoryIds = await _context.Categories
                    .Where(c => c.ParentCategoryID == product.Category.ParentCategoryID)
                    .Select(c => c.CategoryID)
                    .ToListAsync();

                similarProducts = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.ProductID != product.ProductID && siblingCategoryIds.Contains(p.CategoryID))
                    .OrderBy(p => p.Name)
                    .Take(12)
                    .ToListAsync();
            }

            ViewData["SimilarProducts"] = similarProducts;

            return View(product);
        }

        public async Task<IActionResult> Create()
        {
            await LoadCategoryOptionsAsync();
            return View(new Product { CreatedAt = DateTime.UtcNow });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductID,CategoryID,Name,Brand,Color,Price,StockQuantity,ImageUrl,Description,HasSizes,DiscountPercent,CreatedAt")] Product product, List<ProductSizeStockRowViewModel> newSizes, IFormFile? productImage)
        {
            newSizes ??= new List<ProductSizeStockRowViewModel>();

            if (product.CreatedAt == default)
            {
                product.CreatedAt = DateTime.UtcNow;
            }

            if (ModelState.IsValid)
            {
                var uploadedImageUrl = await SaveProductImageAsync(productImage);
                if (!ModelState.IsValid)
                {
                    await LoadCategoryOptionsAsync(product.CategoryID);
                    return View(product);
                }

                if (!string.IsNullOrWhiteSpace(uploadedImageUrl))
                {
                    product.ImageUrl = uploadedImageUrl;
                }

                if (product.HasSizes)
                {
                    var sizesToSave = newSizes
                        .Where(size => !string.IsNullOrWhiteSpace(size.Size))
                        .Select(size => new ProductSize
                        {
                            Size = size.Size!.Trim(),
                            StockQuantity = size.StockQuantity
                        })
                        .ToList();

                    product.ProductSizes = sizesToSave;
                    product.StockQuantity = sizesToSave.Sum(size => size.StockQuantity);
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await LoadCategoryOptionsAsync(product.CategoryID);
            return View(product);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            await LoadCategoryOptionsAsync(product.CategoryID);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductID,CategoryID,Name,Brand,Color,Price,StockQuantity,ImageUrl,Description,HasSizes,DiscountPercent,CreatedAt")] Product product, IFormFile? productImage)
        {
            if (id != product.ProductID)
            {
                return NotFound();
            }

            if (product.CreatedAt == default)
            {
                product.CreatedAt = DateTime.UtcNow;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var uploadedImageUrl = await SaveProductImageAsync(productImage);
                    if (!ModelState.IsValid)
                    {
                        await LoadCategoryOptionsAsync(product.CategoryID);
                        return View(product);
                    }

                    if (!string.IsNullOrWhiteSpace(uploadedImageUrl))
                    {
                        product.ImageUrl = uploadedImageUrl;
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductID))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            await LoadCategoryOptionsAsync(product.CategoryID);
            return View(product);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .ThenInclude(c => c.ParentCategory)
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(m => m.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewData["OrderItemCount"] = await _context.OrderItems.CountAsync(oi => oi.ProductID == id.Value);
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var orderItems = await _context.OrderItems
                .Where(oi => oi.ProductID == id)
                .ToListAsync();

            if (orderItems.Count > 0)
            {
                _context.OrderItems.RemoveRange(orderItems);
            }

            var productSizeIds = product.ProductSizes.Select(ps => ps.ProductSizeID).ToList();
            if (productSizeIds.Count > 0)
            {
                var sizeOrderItems = await _context.OrderItems
                    .Where(oi => oi.ProductSizeID.HasValue && productSizeIds.Contains(oi.ProductSizeID.Value))
                    .ToListAsync();

                if (sizeOrderItems.Count > 0)
                {
                    _context.OrderItems.RemoveRange(sizeOrderItems);
                }

                _context.ProductSizes.RemoveRange(product.ProductSizes);
            }

            try
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Proizvod \"{product.Name}\" je obrisan.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Proizvod se ne može obrisati jer je još povezan sa drugim podacima u bazi.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadCategoryOptionsAsync(int? selectedCategoryId = null)
        {
            var categories = await _context.Categories
                .Include(c => c.ParentCategory)
                .Where(c => c.IsActive)
                .OrderBy(c => c.ParentCategory != null ? c.ParentCategory.DisplayOrder : c.DisplayOrder)
                .ThenBy(c => c.ParentCategory != null ? c.ParentCategory.Name : c.Name)
                .ThenBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var options = categories
                .Where(c => c.ParentCategoryID != null || !categories.Any(child => child.ParentCategoryID == c.CategoryID))
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryID.ToString(),
                    Text = c.ParentCategory == null ? c.Name : $"{c.ParentCategory.Name} > {c.Name}",
                    Selected = selectedCategoryId == c.CategoryID
                })
                .ToList();

            ViewData["CategoryID"] = options;
            ViewData["CategorySizeGroups"] = categories.ToDictionary(
                c => c.CategoryID,
                c => GetCategorySizeGroup(c));
        }

        private async Task LoadCategoryFilterOptionsAsync(int? selectedCategoryId = null)
        {
            var categories = await _context.Categories
                .Include(c => c.ParentCategory)
                .Where(c => c.IsActive)
                .OrderBy(c => c.ParentCategory != null ? c.ParentCategory.DisplayOrder : c.DisplayOrder)
                .ThenBy(c => c.ParentCategory != null ? c.ParentCategory.Name : c.Name)
                .ThenBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var options = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Value = "",
                    Text = "Sve kategorije",
                    Selected = !selectedCategoryId.HasValue
                }
            };

            options.AddRange(categories.Select(c => new SelectListItem
            {
                Value = c.CategoryID.ToString(),
                Text = c.ParentCategory == null ? c.Name : $"{c.ParentCategory.Name} > {c.Name}",
                Selected = selectedCategoryId == c.CategoryID
            }));

            ViewData["CategoryFilterID"] = options;
            ViewData["CurrentCategoryId"] = selectedCategoryId;
        }

        private static string GetCategorySizeGroup(Category category)
        {
            var root = category.ParentCategory ?? category;
            var text = $"{root.Name} {root.Slug} {category.Name} {category.Slug}".ToLowerInvariant();

            if (text.Contains("musk") || text.Contains("mušk"))
            {
                return "men";
            }

            if (text.Contains("zensk") || text.Contains("žensk"))
            {
                return "women";
            }

            return "custom";
        }

        private async Task<string?> SaveProductImageAsync(IFormFile? productImage)
        {
            if (productImage == null || productImage.Length == 0)
            {
                return null;
            }

            if (!productImage.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("ImageUrl", "Možeš dodati samo sliku.");
                return null;
            }

            var extension = Path.GetExtension(productImage.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("ImageUrl", "Slika mora biti JPG, PNG, WEBP ili GIF.");
                return null;
            }

            var uploadFolder = Path.Combine(_environment.WebRootPath, "images", "products");
            Directory.CreateDirectory(uploadFolder);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadFolder, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await productImage.CopyToAsync(stream);

            return $"/images/products/{fileName}";
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductID == id);
        }
    }
}
