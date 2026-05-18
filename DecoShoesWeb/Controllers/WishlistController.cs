using System.Text.Json;
using DecoShoesWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DecoShoesWeb.Controllers
{
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var wishlist = GetWishlist();
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => wishlist.Contains(p.ProductID))
                .ToListAsync();

            return View(products);
        }

        public IActionResult Add(int productId, string? returnUrl)
        {
            var wishlist = GetWishlist();

            if (!wishlist.Contains(productId))
            {
                wishlist.Add(productId);
                SaveWishlist(wishlist);
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Remove(int productId, string? returnUrl)
        {
            var wishlist = GetWishlist();
            wishlist.Remove(productId);
            SaveWishlist(wishlist);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        private List<int> GetWishlist()
        {
            var wishlistJson = HttpContext.Session.GetString("Wishlist");

            if (string.IsNullOrEmpty(wishlistJson))
            {
                return new List<int>();
            }

            return JsonSerializer.Deserialize<List<int>>(wishlistJson) ?? new List<int>();
        }

        private void SaveWishlist(List<int> wishlist)
        {
            HttpContext.Session.SetString("Wishlist", JsonSerializer.Serialize(wishlist));
        }
    }
}
