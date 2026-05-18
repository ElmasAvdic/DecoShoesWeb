using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DecoShoesWeb.Data;
using DecoShoesWeb.Models;

namespace DecoShoesWeb.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.ParentCategory)
                .OrderBy(c => c.ParentCategoryID.HasValue)
                .ThenBy(c => c.ParentCategory != null ? c.ParentCategory.DisplayOrder : c.DisplayOrder)
                .ThenBy(c => c.ParentCategory != null ? c.ParentCategory.Name : c.Name)
                .ThenBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        public async Task<IActionResult> Details(int? id, string? returnUrl = null)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.Subcategories)
                .FirstOrDefaultAsync(m => m.CategoryID == id);

            if (category == null)
            {
                return NotFound();
            }

            ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
            return View(category);
        }

        public async Task<IActionResult> Create()
        {
            await LoadParentCategoriesAsync();
            return View(new Category { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryID,Name,Description,ParentCategoryID,Slug,DisplayOrder,IsActive")] Category category)
        {
            category.Slug = NormalizeSlug(category.Slug, category.Name);

            if (await _context.Categories.AnyAsync(c => c.Slug == category.Slug))
            {
                ModelState.AddModelError(nameof(category.Slug), "Ovaj slug vec postoji.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await LoadParentCategoriesAsync(category.ParentCategoryID);
            return View(category);
        }

        public async Task<IActionResult> Edit(int? id, string? returnUrl = null)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            await LoadParentCategoriesAsync(category.ParentCategoryID, category.CategoryID);
            ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryID,Name,Description,ParentCategoryID,Slug,DisplayOrder,IsActive")] Category category, string? returnUrl = null)
        {
            if (id != category.CategoryID)
            {
                return NotFound();
            }

            if (category.ParentCategoryID == category.CategoryID)
            {
                ModelState.AddModelError(nameof(category.ParentCategoryID), "Kategorija ne moze biti sama sebi roditelj.");
            }

            category.Slug = NormalizeSlug(category.Slug, category.Name);

            if (await _context.Categories.AnyAsync(c => c.Slug == category.Slug && c.CategoryID != category.CategoryID))
            {
                ModelState.AddModelError(nameof(category.Slug), "Ovaj slug vec postoji.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.CategoryID))
                    {
                        return NotFound();
                    }

                    throw;
                }

                var safeReturnUrl = GetSafeReturnUrl(returnUrl);
                return Redirect(safeReturnUrl);
            }

            await LoadParentCategoriesAsync(category.ParentCategoryID, category.CategoryID);
            ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
            return View(category);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.Subcategories)
                .FirstOrDefaultAsync(m => m.CategoryID == id);

            if (category == null)
            {
                return NotFound();
            }

            var categoryIds = new List<int> { category.CategoryID };
            categoryIds.AddRange(category.Subcategories.Select(c => c.CategoryID));

            ViewData["ProductCount"] = await _context.Products.CountAsync(p => categoryIds.Contains(p.CategoryID));
            ViewData["SubcategoryCount"] = category.Subcategories.Count;

            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Subcategories)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var categoryIds = new List<int> { category.CategoryID };
            categoryIds.AddRange(category.Subcategories.Select(c => c.CategoryID));

            var productCount = await _context.Products.CountAsync(p => categoryIds.Contains(p.CategoryID));
            if (productCount > 0)
            {
                TempData["ErrorMessage"] = "Kategorija nije obrisana jer ima proizvode. Prvo prebaci proizvode u drugu kategoriju ili ih obrisi.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            if (category.Subcategories.Any())
            {
                _context.Categories.RemoveRange(category.Subcategories);
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Kategorija je obrisana.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadParentCategoriesAsync(int? selectedParentId = null, int? excludedCategoryId = null)
        {
            var parents = await _context.Categories
                .Where(c => c.ParentCategoryID == null && c.CategoryID != excludedCategoryId)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            ViewData["ParentCategoryID"] = new SelectList(parents, "CategoryID", "Name", selectedParentId);
        }

        private static string NormalizeSlug(string? slug, string name)
        {
            var value = string.IsNullOrWhiteSpace(slug) ? name : slug;
            return value.Trim().ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("č", "c")
                .Replace("ć", "c")
                .Replace("š", "s")
                .Replace("đ", "dj")
                .Replace("ž", "z");
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryID == id);
        }

        private string GetSafeReturnUrl(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return returnUrl;
            }

            return Url.Action(nameof(Index)) ?? "/Categories";
        }
    }
}
