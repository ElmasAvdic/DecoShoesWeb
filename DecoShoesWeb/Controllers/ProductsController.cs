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

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Products
                .Include(p => p.Category)
                .ThenInclude(c => c.ParentCategory)
                .Include(p => p.ProductSizes);

            return View(await applicationDbContext.ToListAsync());
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

            return View(product);
        }

        public async Task<IActionResult> Create()
        {
            await LoadCategoryOptionsAsync();
            return View(new Product { CreatedAt = DateTime.UtcNow });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductID,CategoryID,Name,Brand,Color,Price,StockQuantity,ImageUrl,Description,HasSizes,DiscountPercent,CreatedAt")] Product product)
        {
            if (product.CreatedAt == default)
            {
                product.CreatedAt = DateTime.UtcNow;
            }

            if (ModelState.IsValid)
            {
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
        public async Task<IActionResult> Edit(int id, [Bind("ProductID,CategoryID,Name,Brand,Color,Price,StockQuantity,ImageUrl,Description,HasSizes,DiscountPercent,CreatedAt")] Product product)
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
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductID == id);
        }
    }
}
