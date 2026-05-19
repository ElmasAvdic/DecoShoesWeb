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
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Payments
        public async Task<IActionResult> Index(int? orderId, string? status, string? search)
        {
            var payments = _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o!.Customer)
                .AsQueryable();

            if (orderId.HasValue)
            {
                payments = payments.Where(p => p.OrderID == orderId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                payments = payments.Where(p => p.PaymentStatus == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                payments = payments.Where(p =>
                    p.OrderID.ToString().Contains(term) ||
                    (p.PaymentMethod != null && p.PaymentMethod.Contains(term)) ||
                    (p.Order != null && p.Order.Customer != null &&
                        (p.Order.Customer.FirstName.Contains(term) || p.Order.Customer.LastName.Contains(term))));
            }

            await LoadOrderFilterOptionsAsync(orderId);
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentStatus"] = status;

            return View(await payments
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.PaymentID)
                .ToListAsync());
        }

        // GET: Payments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(m => m.PaymentID == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // GET: Payments/Create
        public IActionResult Create()
        {
            LoadOrderOptions();
            return View(new Payment { PaymentDate = DateTime.Now, PaymentStatus = "Na čekanju" });
        }

        // POST: Payments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PaymentID,OrderID,PaymentDate,Amount,PaymentMethod,PaymentStatus")] Payment payment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(payment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            LoadOrderOptions(payment.OrderID);
            return View(payment);
        }

        // GET: Payments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }
            LoadOrderOptions(payment.OrderID);
            return View(payment);
        }

        // POST: Payments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PaymentID,OrderID,PaymentDate,Amount,PaymentMethod,PaymentStatus")] Payment payment)
        {
            if (id != payment.PaymentID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(payment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentExists(payment.PaymentID))
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
            LoadOrderOptions(payment.OrderID);
            return View(payment);
        }

        // GET: Payments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(m => m.PaymentID == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.PaymentID == id);
        }

        private void LoadOrderOptions(int? selectedOrderId = null)
        {
            var orders = _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .ToList()
                .Select(o => new
                {
                    o.OrderID,
                    DisplayName = "#" + o.OrderID + " - " + (o.Customer != null ? o.Customer.FirstName + " " + o.Customer.LastName : "Kupac") + " - " + o.TotalAmount.ToString("0.00") + " KM"
                });

            ViewData["OrderID"] = new SelectList(orders, "OrderID", "DisplayName", selectedOrderId);
        }

        private async Task LoadOrderFilterOptionsAsync(int? selectedOrderId = null)
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    o.OrderID,
                    DisplayName = "#" + o.OrderID + " - " + (o.Customer != null ? o.Customer.FirstName + " " + o.Customer.LastName : "Kupac")
                })
                .ToListAsync();

            var options = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Sve narudžbe", Selected = !selectedOrderId.HasValue }
            };

            options.AddRange(orders.Select(o => new SelectListItem
            {
                Value = o.OrderID.ToString(),
                Text = o.DisplayName,
                Selected = selectedOrderId == o.OrderID
            }));

            ViewData["OrderFilterID"] = options;
        }
    }
}
