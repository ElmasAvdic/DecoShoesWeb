using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using DecoShoesWeb.Data;
using DecoShoesWeb.Models;

namespace DecoShoesWeb.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int productSizeId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            var productSize = await _context.ProductSizes.FindAsync(productSizeId);

            if (product == null || productSize == null)
            {
                return NotFound();
            }

            if (quantity > productSize.StockQuantity)
            {
                TempData["Message"] = "Nema dovoljno zalihe za odabranu veličinu.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            var cart = GetCart();

            var existingItem = cart.FirstOrDefault(x => x.ProductSizeId == productSizeId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = productId,
                    ProductSizeId = productSizeId,
                    ProductName = product.Name,
                    ImageUrl = product.ImageUrl,
                    Size = productSize.Size,
                    Quantity = quantity,
                    Price = product.Price
                });
            }

            SaveCart(cart);

            return RedirectToAction("Index");
        }

        public IActionResult Remove(int productSizeId)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(x => x.ProductSizeId == productSizeId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }

            return RedirectToAction("Index");
        }

        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");

            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }

            return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString("Cart", cartJson);
        }
    }
}