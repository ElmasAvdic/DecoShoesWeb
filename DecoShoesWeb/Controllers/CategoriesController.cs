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
                // Ensure DisplayOrder is set and shift others if necessary (grouped by ParentCategoryID)
                if (category.DisplayOrder <= 0)
                {
                    var maxOrder = await _context.Categories
                        .Where(c => c.ParentCategoryID == category.ParentCategoryID)
                        .MaxAsync(c => (int?)c.DisplayOrder) ?? 0;
                    category.DisplayOrder = maxOrder + 1;
                }
                else
                {
                    // Shift existing categories with same parent that are >= this order
                    var toShift = await _context.Categories
                        .Where(c => c.ParentCategoryID == category.ParentCategoryID && c.DisplayOrder >= category.DisplayOrder)
                        .ToListAsync();

                    foreach (var c in toShift)
                    {
                        c.DisplayOrder += 1;
                        _context.Update(c);
                    }
                }

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
                    // Handle reordering when DisplayOrder or ParentCategoryID changed
                    var existing = await _context.Categories.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.CategoryID == category.CategoryID);

                    if (existing == null)
                    {
                        return NotFound();
                    }

                    var oldParent = existing.ParentCategoryID;
                    var oldOrder = existing.DisplayOrder;
                    var newParent = category.ParentCategoryID;
                    var newOrder = category.DisplayOrder <= 0 ? oldOrder : category.DisplayOrder;

                    // If parent changed, remove gap from old parent
                    if (oldParent != newParent)
                    {
                        var siblingsOld = await _context.Categories
                            .Where(c => c.ParentCategoryID == oldParent && c.DisplayOrder > oldOrder && c.CategoryID != category.CategoryID)
                            .ToListAsync();

                        foreach (var s in siblingsOld)
                        {
                            s.DisplayOrder -= 1;
                            _context.Update(s);
                        }

                        // Shift in new parent for positions >= newOrder
                        var siblingsNew = await _context.Categories
                            .Where(c => c.ParentCategoryID == newParent && c.DisplayOrder >= newOrder)
                            .ToListAsync();

                        foreach (var s in siblingsNew)
                        {
                            s.DisplayOrder += 1;
                            _context.Update(s);
                        }
                    }
                    else if (oldOrder != newOrder)
                    {
                        if (newOrder > oldOrder)
                        {
                            // Shift down items between oldOrder+1 .. newOrder (decrement)
                            var toDecrement = await _context.Categories
                                .Where(c => c.ParentCategoryID == oldParent && c.DisplayOrder > oldOrder && c.DisplayOrder <= newOrder && c.CategoryID != category.CategoryID)
                                .ToListAsync();

                            foreach (var s in toDecrement)
                            {
                                s.DisplayOrder -= 1;
                                _context.Update(s);
                            }
                        }
                        else
                        {
                            // newOrder < oldOrder -> increment items between newOrder .. oldOrder-1
                            var toIncrement = await _context.Categories
                                .Where(c => c.ParentCategoryID == oldParent && c.DisplayOrder >= newOrder && c.DisplayOrder < oldOrder && c.CategoryID != category.CategoryID)
                                .ToListAsync();

                            foreach (var s in toIncrement)
                            {
                                s.DisplayOrder += 1;
                                _context.Update(s);
                            }
                        }
                    }

                    // Finally update the category itself
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

            // Remember parent and order to fix sibling DisplayOrder
            var parentId = category.ParentCategoryID;
            var removedOrder = category.DisplayOrder;

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            // Decrement display order of siblings that were after the removed category
            var siblingsToFix = await _context.Categories
                .Where(c => c.ParentCategoryID == parentId && c.DisplayOrder > removedOrder)
                .ToListAsync();

            foreach (var s in siblingsToFix)
            {
                s.DisplayOrder -= 1;
                _context.Update(s);
            }

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
