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

        public async Task<IActionResult> Index(int? categoryId)
        {
            // Uèitaj sve kategorije
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            if (categoryId.HasValue && !categories.Any(c => c.CategoryID == categoryId.Value))
            {
                categoryId = null;
            }
            ViewData["Categories"] = categories;
            ViewData["SelectedCategoryId"] = categoryId;

            // Uèitaj proizvode
            IQueryable<Product> products = _context.Products.Include(p => p.Category);

            // Filtriraj po kategoriji ako je odabrana
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                products = products.Where(p => p.CategoryID == categoryId.Value);
            }

            // Sortiraj po cijeni (od nižih prema višim)
            var productList = await products
                .OrderBy(p => p.Price)
                .ThenBy(p => p.Name)
                .ToListAsync();

            return View(productList);
        }

        public IActionResult Privacy()
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
