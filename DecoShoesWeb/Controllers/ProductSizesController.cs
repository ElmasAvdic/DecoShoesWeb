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
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ProductSizes
                .Include(p => p.Product)
                .OrderBy(p => p.Product!.Name)
                .ThenBy(p => p.Size);

            return View(await applicationDbContext.ToListAsync());
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
            ViewData["ProductID"] = new SelectList(_context.Products, "ProductID", "Name");
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
            ViewData["ProductID"] = new SelectList(_context.Products, "ProductID", "Name", productSize.ProductID);
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
            ViewData["ProductID"] = new SelectList(_context.Products, "ProductID", "Name", productSize.ProductID);
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
            ViewData["ProductID"] = new SelectList(_context.Products, "ProductID", "Name", productSize.ProductID);
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
                _context.Update(product);
                await _context.SaveChangesAsync();
            }
        }

    }
}
