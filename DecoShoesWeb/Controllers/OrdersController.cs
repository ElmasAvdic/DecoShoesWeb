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
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index(int? customerId, string? status, string? search)
        {
            var orders = _context.Orders
                .Include(o => o.Customer)
                .AsQueryable();

            if (customerId.HasValue)
            {
                orders = orders.Where(o => o.CustomerID == customerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                orders = orders.Where(o => o.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                orders = orders.Where(o =>
                    o.OrderID.ToString().Contains(term) ||
                    (o.Customer != null && (o.Customer.FirstName.Contains(term) || o.Customer.LastName.Contains(term) ||
                    (o.Customer.Email != null && o.Customer.Email.Contains(term)) ||
                    (o.Customer.Phone != null && o.Customer.Phone.Contains(term)))));
            }

            await LoadCustomerOptionsAsync(customerId);
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentStatus"] = status;

            return View(await orders
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderID)
                .ToListAsync());
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(m => m.OrderID == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            LoadCustomerOptions();
            return View(new Order { OrderDate = DateTime.Now, Status = "Nova" });
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderID,CustomerID,OrderDate,TotalAmount,Status")] Order order)
        {
            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            LoadCustomerOptions(order.CustomerID);
            return View(order);
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            LoadCustomerOptions(order.CustomerID);
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderID,CustomerID,OrderDate,TotalAmount,Status")] Order order)
        {
            if (id != order.OrderID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.OrderID))
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
            LoadCustomerOptions(order.CustomerID);
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(m => m.OrderID == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderID == id);
        }

        private void LoadCustomerOptions(int? selectedCustomerId = null)
        {
            var customers = _context.Customers
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Select(c => new
                {
                    c.CustomerID,
                    DisplayName = c.FirstName + " " + c.LastName + (c.Phone != null ? " - " + c.Phone : "")
                })
                .ToList();

            ViewData["CustomerID"] = new SelectList(customers, "CustomerID", "DisplayName", selectedCustomerId);
        }

        private async Task LoadCustomerOptionsAsync(int? selectedCustomerId = null)
        {
            var customers = await _context.Customers
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Select(c => new
                {
                    c.CustomerID,
                    DisplayName = c.FirstName + " " + c.LastName + (c.Phone != null ? " - " + c.Phone : "")
                })
                .ToListAsync();

            var options = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Svi kupci", Selected = !selectedCustomerId.HasValue }
            };

            options.AddRange(customers.Select(c => new SelectListItem
            {
                Value = c.CustomerID.ToString(),
                Text = c.DisplayName,
                Selected = selectedCustomerId == c.CustomerID
            }));

            ViewData["CustomerFilterID"] = options;
        }
    }
}
