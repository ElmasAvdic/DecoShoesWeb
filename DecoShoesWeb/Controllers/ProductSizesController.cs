using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DecoShoesWeb.Data;
using DecoShoesWeb.Models;

namespace DecoShoesWeb.Controllers
{
    public class ProductSizesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductSizesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ProductSizes
        public async Task<IActionResult> Index(int? productId, int? categoryId, string? search)
        {
            var productSizes = _context.ProductSizes
                .Include(p => p.Product)
                .ThenInclude(p => p!.Category)
                .ThenInclude(c => c!.ParentCategory)
                .AsQueryable();

            if (productId.HasValue)
            {
                productSizes = productSizes.Where(ps => ps.ProductID == productId.Value);
            }

            if (categoryId.HasValue)
            {
                productSizes = productSizes.Where(ps =>
                    ps.Product != null &&
                    (ps.Product.CategoryID == categoryId.Value || ps.Product.Category!.ParentCategoryID == categoryId.Value));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                productSizes = productSizes.Where(ps =>
                    ps.Size.Contains(term) ||
                    (ps.Product != null && ps.Product.Name.Contains(term)) ||
                    (ps.Product != null && ps.Product.Brand != null && ps.Product.Brand.Contains(term)) ||
                    (ps.Product != null && ps.Product.Color != null && ps.Product.Color.Contains(term)) ||
                    (ps.Product != null && ps.Product.Category != null && ps.Product.Category.Name.Contains(term)) ||
                    (ps.Product != null && ps.Product.Category != null && ps.Product.Category.ParentCategory != null && ps.Product.Category.ParentCategory.Name.Contains(term)));
            }

            await LoadProductFilterOptionsAsync(productId);
            await LoadCategoryFilterOptionsAsync(categoryId);
            ViewData["CurrentSearch"] = search;

            return View(await productSizes
                .OrderBy(p => p.Product!.Category!.ParentCategory != null ? p.Product.Category.ParentCategory.DisplayOrder : p.Product.Category.DisplayOrder)
                .ThenBy(p => p.Product!.Category!.ParentCategory != null ? p.Product.Category.ParentCategory.Name : p.Product.Category.Name)
                .ThenBy(p => p.Product!.Category!.DisplayOrder)
                .ThenBy(p => p.Product!.Name)
                .ThenBy(p => p.Size)
                .ToListAsync());
        }

        public async Task<IActionResult> Manage(int productId)
        {
            var product = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductID == productId);

            if (product == null)
            {
                return NotFound();
            }

            return View(BuildManageModel(product));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(ProductSizeStockManageViewModel model)
        {
            var product = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductID == model.ProductID);

            if (product == null)
            {
                return NotFound();
            }

            model.ProductName = product.Name;

            foreach (var existingSize in model.ExistingSizes)
            {
                if (existingSize.ProductSizeID == null)
                {
                    continue;
                }

                var productSize = product.ProductSizes
                    .FirstOrDefault(ps => ps.ProductSizeID == existingSize.ProductSizeID.Value);

                if (productSize == null)
                {
                    continue;
                }

                if (existingSize.Delete)
                {
                    _context.ProductSizes.Remove(productSize);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(existingSize.Size))
                {
                    ModelState.AddModelError("", "Broj ne smije biti prazan.");
                    continue;
                }

                productSize.Size = existingSize.Size.Trim();
                productSize.StockQuantity = existingSize.StockQuantity;
            }

            foreach (var newSize in model.NewSizes)
            {
                if (string.IsNullOrWhiteSpace(newSize.Size))
                {
                    continue;
                }

                _context.ProductSizes.Add(new ProductSize
                {
                    ProductID = product.ProductID,
                    Size = newSize.Size.Trim(),
                    StockQuantity = newSize.StockQuantity
                });
            }

            if (!ModelState.IsValid)
            {
                model.ExistingSizes = product.ProductSizes
                    .OrderBy(ps => ps.Size)
                    .Select(ps => new ProductSizeStockRowViewModel
                    {
                        ProductSizeID = ps.ProductSizeID,
                        Size = ps.Size,
                        StockQuantity = ps.StockQuantity
                    })
                    .ToList();

                return View(model);
            }

            product.HasSizes = true;
            await _context.SaveChangesAsync();
            await UpdateProductStockQuantity(product.ProductID);

            TempData["Message"] = $"Brojevi za \"{product.Name}\" su sačuvani.";
            return RedirectToAction("Index", "Products");
        }

        // GET: ProductSizes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productSize = await _context.ProductSizes
                .Include(p => p.Product)
                .FirstOrDefaultAsync(m => m.ProductSizeID == id);
            if (productSize == null)
            {
                return NotFound();
            }

            return View(productSize);
        }

        // GET: ProductSizes/Create
        public IActionResult Create()
        {
            LoadProductOptions();
            return View();
        }

        // POST: ProductSizes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductSizeID,ProductID,Size,StockQuantity")] ProductSize productSize)
        {
            if (ModelState.IsValid)
            {
                _context.Add(productSize);
                await _context.SaveChangesAsync();
                await UpdateProductStockQuantity(productSize.ProductID);
                return RedirectToAction(nameof(Index));
            }
            LoadProductOptions(productSize.ProductID);
            return View(productSize);
        }

        // GET: ProductSizes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productSize = await _context.ProductSizes.FindAsync(id);
            if (productSize == null)
            {
                return NotFound();
            }
            LoadProductOptions(productSize.ProductID);
            return View(productSize);
        }

        // POST: ProductSizes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductSizeID,ProductID,Size,StockQuantity")] ProductSize productSize)
        {
            if (id != productSize.ProductSizeID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(productSize);
                    await _context.SaveChangesAsync();
                    await UpdateProductStockQuantity(productSize.ProductID);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductSizeExists(productSize.ProductSizeID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            LoadProductOptions(productSize.ProductID);
            return View(productSize);
        }

        // GET: ProductSizes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productSize = await _context.ProductSizes
                .Include(p => p.Product)
                .FirstOrDefaultAsync(m => m.ProductSizeID == id);
            if (productSize == null)
            {
                return NotFound();
            }

            return View(productSize);
        }

        // POST: ProductSizes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var productSize = await _context.ProductSizes.FindAsync(id);
            if (productSize != null)
            {
                int productId = productSize.ProductID;
                _context.ProductSizes.Remove(productSize);
                await _context.SaveChangesAsync();
                await UpdateProductStockQuantity(productId);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProductSizeExists(int id)
        {
            return _context.ProductSizes.Any(e => e.ProductSizeID == id);
        }

        private void LoadProductOptions(int? selectedProductId = null)
        {
            ViewData["ProductID"] = new SelectList(
                _context.Products.OrderBy(p => p.Name).ToList(),
                "ProductID",
                "Name",
                selectedProductId);
        }

        private async Task LoadProductFilterOptionsAsync(int? selectedProductId = null)
        {
            var products = await _context.Products
                .Where(p => p.HasSizes || p.ProductSizes.Any())
                .OrderBy(p => p.Name)
                .ToListAsync();

            var options = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Value = "",
                    Text = "Svi proizvodi",
                    Selected = !selectedProductId.HasValue
                }
            };

            options.AddRange(products.Select(p => new SelectListItem
            {
                Value = p.ProductID.ToString(),
                Text = p.Name,
                Selected = selectedProductId == p.ProductID
            }));

            ViewData["ProductFilterID"] = options;
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
        }

        private ProductSizeStockManageViewModel BuildManageModel(Product product)
        {
            return new ProductSizeStockManageViewModel
            {
                ProductID = product.ProductID,
                ProductName = product.Name,
                ExistingSizes = product.ProductSizes
                    .OrderBy(ps => ps.Size)
                    .Select(ps => new ProductSizeStockRowViewModel
                    {
                        ProductSizeID = ps.ProductSizeID,
                        Size = ps.Size,
                        StockQuantity = ps.StockQuantity
                    })
                    .ToList(),
                NewSizes = Enumerable.Range(0, 6)
                    .Select(_ => new ProductSizeStockRowViewModel())
                    .ToList()
            };
        }

        private async Task UpdateProductStockQuantity(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                var totalStockInSizes = await _context.ProductSizes
                    .Where(ps => ps.ProductID == productId)
                    .SumAsync(ps => ps.StockQuantity);

                product.StockQuantity = totalStockInSizes;
                product.HasSizes = await _context.ProductSizes.AnyAsync(ps => ps.ProductID == productId);
                _context.Update(product);
                await _context.SaveChangesAsync();
            }
        }

    }
}
