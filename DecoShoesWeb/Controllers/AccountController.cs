using DecoShoesWeb.Data;
using DecoShoesWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DecoShoesWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["AccountModal"] = "login";
                TempData["AccountError"] = "Upiši email adresu za prijavu.";
                return RedirectToAction("Index", "Home");
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email != null && c.Email == model.Email);

            if (customer == null)
            {
                TempData["AccountModal"] = "login";
                TempData["AccountError"] = "Nismo pronašli račun sa tim emailom.";
                return RedirectToAction("Index", "Home");
            }

            HttpContext.Session.SetInt32("CustomerID", customer.CustomerID);
            HttpContext.Session.SetString("CustomerName", customer.FirstName);

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register()
        {
            return View(new Customer());
        }

        public async Task<IActionResult> MyOrders()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerID");
            if (!customerId.HasValue)
            {
                TempData["AccountModal"] = "login";
                return RedirectToAction("Index", "Home");
            }

            var orders = await _context.Orders
                .Where(o => o.CustomerID == customerId.Value)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> Profile()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerID");
            if (!customerId.HasValue)
            {
                TempData["AccountModal"] = "login";
                return RedirectToAction("Index", "Home");
            }

            var customer = await _context.Customers.FindAsync(customerId.Value);
            if (customer == null)
            {
                HttpContext.Session.Remove("CustomerID");
                HttpContext.Session.Remove("CustomerName");
                TempData["AccountModal"] = "login";
                return RedirectToAction("Index", "Home");
            }

            return View(customer);
        }

        public async Task<IActionResult> EditProfile()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerID");
            if (!customerId.HasValue)
            {
                TempData["AccountModal"] = "login";
                return RedirectToAction("Index", "Home");
            }

            var customer = await _context.Customers.FindAsync(customerId.Value);
            if (customer == null)
            {
                HttpContext.Session.Remove("CustomerID");
                HttpContext.Session.Remove("CustomerName");
                TempData["AccountModal"] = "login";
                return RedirectToAction("Index", "Home");
            }

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile([Bind("CustomerID,FirstName,LastName,Phone,Email,Address,City,PostalCode")] Customer customer)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerID");
            if (!customerId.HasValue || customer.CustomerID != customerId.Value)
            {
                TempData["AccountModal"] = "login";
                return RedirectToAction("Index", "Home");
            }

            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                var emailExists = await _context.Customers
                    .AnyAsync(c => c.CustomerID != customer.CustomerID && c.Email != null && c.Email == customer.Email);

                if (emailExists)
                {
                    ModelState.AddModelError(nameof(Customer.Email), "Već postoji račun sa tim emailom.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            _context.Update(customer);
            await _context.SaveChangesAsync();
            HttpContext.Session.SetString("CustomerName", customer.FirstName);

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("CustomerID,FirstName,LastName,Phone,Email,Address,City,PostalCode")] Customer customer)
        {
            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                var emailExists = await _context.Customers
                    .AnyAsync(c => c.Email != null && c.Email == customer.Email);

                if (emailExists)
                {
                    ModelState.AddModelError(nameof(Customer.Email), "Već postoji račun sa tim emailom.");
                }
            }

            if (!ModelState.IsValid)
            {
                TempData["AccountModal"] = "register";
                TempData["AccountError"] = "Provjeri podatke za registraciju.";
                return RedirectToAction("Index", "Home");
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("CustomerID", customer.CustomerID);
            HttpContext.Session.SetString("CustomerName", customer.FirstName);

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("CustomerID");
            HttpContext.Session.Remove("CustomerName");

            return RedirectToAction("Index", "Home");
        }
    }
}
