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
            return View(GetCart());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int? productSizeId, int quantity, string? returnUrl)
        {
            var product = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductID == productId);

            if (product == null)
            {
                return NotFound();
            }

            if (quantity < 1)
            {
                TempData["Message"] = "Količina mora biti najmanje 1.";
                return RedirectToAction("Details", "Products", new { id = productId, source = "shop" });
            }

            var cart = GetCart();
            var unitPrice = GetEffectivePrice(product);

            if (product.HasSizes)
            {
                if (productSizeId == null)
                {
                    TempData["Message"] = "Molimo izaberite veličinu.";
                    return RedirectToAction("Details", "Products", new { id = productId, source = "shop" });
                }

                var productSize = product.ProductSizes.FirstOrDefault(ps => ps.ProductSizeID == productSizeId.Value);
                if (productSize == null)
                {
                    return NotFound();
                }

                var existingItem = cart.FirstOrDefault(x => x.ProductId == productId && x.ProductSizeId == productSizeId.Value);
                var requestedQuantity = quantity + (existingItem?.Quantity ?? 0);

                if (requestedQuantity > productSize.StockQuantity)
                {
                    TempData["Message"] = "Nema dovoljno zalihe za odabranu veličinu.";
                    return RedirectToAction("Details", "Products", new { id = productId, source = "shop" });
                }

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    existingItem.Price = unitPrice;
                }
                else
                {
                    cart.Add(new CartItem
                    {
                        ProductId = productId,
                        ProductSizeId = productSize.ProductSizeID,
                        ProductName = product.Name,
                        ImageUrl = product.ImageUrl,
                        Size = productSize.Size,
                        Quantity = quantity,
                        Price = unitPrice
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
                    return RedirectToAction("Details", "Products", new { id = productId, source = "shop" });
                }

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    existingItem.Price = unitPrice;
                }
                else
                {
                    cart.Add(new CartItem
                    {
                        ProductId = productId,
                        ProductName = product.Name,
                        ImageUrl = product.ImageUrl,
                        Quantity = quantity,
                        Price = unitPrice
                    });
                }
            }

            SaveCart(cart);
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int productId, int? productSizeId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == productId && x.ProductSizeId == productSizeId);

            if (item == null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (quantity < 1)
            {
                cart.Remove(item);
                SaveCart(cart);
                return RedirectToAction(nameof(Index));
            }

            var stock = await GetAvailableStockAsync(productId, productSizeId);
            if (quantity > stock)
            {
                TempData["Message"] = "Nema dovoljno zalihe za traženu količinu.";
                return RedirectToAction(nameof(Index));
            }

            item.Quantity = quantity;
            SaveCart(cart);
            return RedirectToAction(nameof(Index));
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

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            var model = new CheckoutViewModel { Items = cart };
            var customerId = HttpContext.Session.GetInt32("CustomerID");

            if (customerId.HasValue)
            {
                var customer = await _context.Customers.FindAsync(customerId.Value);
                if (customer != null)
                {
                    model.FirstName = customer.FirstName;
                    model.LastName = customer.LastName;
                    model.Email = customer.Email ?? "";
                    model.Phone = customer.Phone ?? "";
                    model.Address = customer.Address ?? "";
                    model.City = customer.City ?? "";
                    model.PostalCode = customer.PostalCode;
                    model.SaveCustomerInfo = true;
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var cart = GetCart();
            model.Items = cart;

            if (!cart.Any())
            {
                ModelState.AddModelError("", "Korpa je prazna.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var item in cart)
                {
                    var stock = await GetAvailableStockAsync(item.ProductId, item.ProductSizeId);
                    if (item.Quantity > stock)
                    {
                        ModelState.AddModelError("", $"Nema dovoljno zalihe za {item.ProductName}.");
                        return View(model);
                    }
                }

                var customer = await ResolveCustomerAsync(model);
                var total = cart.Sum(item => item.Total);
                var isCardPayment = model.PaymentMethod == "Kartično plaćanje";

                var order = new Order
                {
                    CustomerID = customer.CustomerID,
                    OrderDate = DateTime.Now,
                    TotalAmount = total,
                    Status = isCardPayment ? "Čeka plaćanje" : "Nova"
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in cart)
                {
                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderID = order.OrderID,
                        ProductID = item.ProductId,
                        ProductSizeID = item.ProductSizeId,
                        Size = item.Size,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price
                    });

                    await ReduceStockAsync(item);
                }

                _context.Payments.Add(new Payment
                {
                    OrderID = order.OrderID,
                    PaymentDate = DateTime.Now,
                    Amount = total,
                    PaymentMethod = model.PaymentMethod,
                    PaymentStatus = model.PaymentMethod switch
                    {
                        "Kartično plaćanje" => "Čeka kartično plaćanje",
                        "Plaćanje pouzećem" => "Na čekanju",
                        _ => "Evidentirano"
                    }
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (model.SaveCustomerInfo)
                {
                    HttpContext.Session.SetInt32("CustomerID", customer.CustomerID);
                    HttpContext.Session.SetString("CustomerName", customer.FirstName);
                }
                SaveCart(new List<CartItem>());

                return RedirectToAction(nameof(Confirmation), new { id = order.OrderID });
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Narudžba nije završena. Pokušaj ponovo.");
                return View(model);
            }
        }

        public async Task<IActionResult> Confirmation(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderID == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        private async Task<Customer> ResolveCustomerAsync(CheckoutViewModel model)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerID");
            Customer? customer = null;

            if (model.SaveCustomerInfo && customerId.HasValue)
            {
                customer = await _context.Customers.FindAsync(customerId.Value);
            }

            if (model.SaveCustomerInfo)
            {
                customer ??= await _context.Customers.FirstOrDefaultAsync(c => c.Email != null && c.Email == model.Email);
            }

            if (customer == null)
            {
                customer = new Customer();
                _context.Customers.Add(customer);
            }

            customer.FirstName = model.FirstName.Trim();
            customer.LastName = model.LastName.Trim();
            customer.Email = model.Email.Trim();
            customer.Phone = model.Phone.Trim();
            customer.Address = model.Address.Trim();
            customer.City = model.City.Trim();
            customer.PostalCode = model.PostalCode?.Trim();

            await _context.SaveChangesAsync();
            return customer;
        }

        private async Task<int> GetAvailableStockAsync(int productId, int? productSizeId)
        {
            if (productSizeId.HasValue)
            {
                var productSize = await _context.ProductSizes.FindAsync(productSizeId.Value);
                return productSize?.StockQuantity ?? 0;
            }

            var product = await _context.Products.FindAsync(productId);
            return product?.StockQuantity ?? 0;
        }

        private async Task ReduceStockAsync(CartItem item)
        {
            if (item.ProductSizeId.HasValue)
            {
                var productSize = await _context.ProductSizes.FindAsync(item.ProductSizeId.Value);
                if (productSize != null)
                {
                    productSize.StockQuantity -= item.Quantity;
                    _context.Update(productSize);

                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity = await _context.ProductSizes
                            .Where(ps => ps.ProductID == item.ProductId)
                            .Where(ps => ps.ProductSizeID != productSize.ProductSizeID)
                            .SumAsync(ps => ps.StockQuantity) + productSize.StockQuantity;
                        _context.Update(product);
                    }
                }
            }
            else
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity = Math.Max(0, (product.StockQuantity ?? 0) - item.Quantity);
                    _context.Update(product);
                }
            }
        }

        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            return string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
        }

        private static decimal GetEffectivePrice(Product product)
        {
            return product.DiscountPercent.HasValue && product.DiscountPercent > 0
                ? product.Price * (1 - product.DiscountPercent.Value / 100)
                : product.Price;
        }
    }
}
