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
        public async Task<IActionResult> AddToCart(int productId, int? productSizeId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);

            if (product == null)
            {
                return NotFound();
            }

            if (quantity < 1)
            {
                TempData["Message"] = "Količina mora biti najmanje 1.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            var cart = GetCart();

            if (product.HasSizes)
            {
                if (productSizeId == null)
                {
                    TempData["Message"] = "Molimo izaberite veličinu.";
                    return RedirectToAction("Details", "Products", new { id = productId });
                }

                var productSize = await _context.ProductSizes.FindAsync(productSizeId.Value);

                if (productSize == null || productSize.ProductID != productId)
                {
                    return NotFound();
                }

                var existingItem = cart.FirstOrDefault(x => x.ProductId == productId && x.ProductSizeId == productSizeId.Value);
                var requestedQuantity = quantity + (existingItem?.Quantity ?? 0);

                if (requestedQuantity > productSize.StockQuantity)
                {
                    TempData["Message"] = "Nema dovoljno zalihe za odabranu veličinu.";
                    return RedirectToAction("Details", "Products", new { id = productId });
                }

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    cart.Add(new CartItem
                    {
                        ProductId = productId,
                        ProductSizeId = productSizeId.Value,
                        ProductName = product.Name,
                        ImageUrl = product.ImageUrl,
                        Size = productSize.Size,
                        Quantity = quantity,
                        Price = product.Price
                    });
                }
            }
            else
            {
                var stockQuantity = product.StockQuantity ?? 0;

                var existingItem = cart.FirstOrDefault(x => x.ProductId == productId && x.ProductSizeId == null);
                var requestedQuantity = quantity + (existingItem?.Quantity ?? 0);

                if (requestedQuantity > stockQuantity)
                {
                    TempData["Message"] = "Nema dovoljno zalihe za odabrani proizvod.";
                    return RedirectToAction("Details", "Products", new { id = productId });
                }

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    cart.Add(new CartItem
                    {
                        ProductId = productId,
                        ProductName = product.Name,
                        ImageUrl = product.ImageUrl,
                        Quantity = quantity,
                        Price = product.Price
                    });
                }
            }

            SaveCart(cart);

            return RedirectToAction("Index");
        }

        public IActionResult Remove(int productId, int? productSizeId)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(x => x.ProductId == productId && x.ProductSizeId == productSizeId);

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
