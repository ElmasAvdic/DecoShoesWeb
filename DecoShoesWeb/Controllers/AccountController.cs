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
                return View(model);
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email != null && c.Email == model.Email);

            if (customer == null)
            {
                ModelState.AddModelError("", "Nismo pronašli račun sa tim emailom.");
                return View(model);
            }

            HttpContext.Session.SetInt32("CustomerID", customer.CustomerID);
            HttpContext.Session.SetString("CustomerName", customer.FirstName);

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register()
        {
            return View(new Customer());
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
                return View(customer);
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
