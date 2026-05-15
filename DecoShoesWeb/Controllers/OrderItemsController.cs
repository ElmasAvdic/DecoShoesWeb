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
    public class OrderItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: OrderItems
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.OrderItems.Include(o => o.Order).Include(o => o.Product);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: OrderItems/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderItem = await _context.OrderItems
                .Include(o => o.Order)
                .Include(o => o.Product)
                .FirstOrDefaultAsync(m => m.OrderItemID == id);
            if (orderItem == null)
            {
                return NotFound();
            }

            return View(orderItem);
        }

        // GET: OrderItems/Create
        public IActionResult Create()
        {
            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID");
            ViewData["ProductID"] = new SelectList(_context.Products, "ProductID", "Name");
            return View();
        }

        // POST: OrderItems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderItemID,OrderID,ProductID,ProductSizeID,Size,Quantity,UnitPrice")] OrderItem orderItem)
        {
            if (ModelState.IsValid)
            {
                var product = await _context.Products.FindAsync(orderItem.ProductID);

                if (product == null)
                {
                    return NotFound();
                }

                if (product.HasSizes)
                {
                    var productSize = await _context.ProductSizes
                        .FirstOrDefaultAsync(ps => ps.ProductID == orderItem.ProductID && ps.Size == orderItem.Size);

                    if (productSize == null)
                    {
                        ModelState.AddModelError("", "This size does not exist for selected product.");
                    }
                    else if (productSize.StockQuantity < orderItem.Quantity)
                    {
                        ModelState.AddModelError("", "Not enough stock for selected size.");
                    }
                    else
                    {
                        productSize.StockQuantity -= orderItem.Quantity;
                        orderItem.ProductSizeID = productSize.ProductSizeID;

                        _context.Update(productSize);
                        _context.Add(orderItem);
                        await _context.SaveChangesAsync();

                        // Update product stock quantity
                        await UpdateProductStockQuantity(orderItem.ProductID);

                        return RedirectToAction(nameof(Index));
                    }
                }
                else
                {
                    if (product.StockQuantity == null || product.StockQuantity < orderItem.Quantity)
                    {
                        ModelState.AddModelError("", "Not enough stock for selected product.");
                    }
                    else
                    {
                        product.StockQuantity -= orderItem.Quantity;

                        _context.Update(product);
                        _context.Add(orderItem);
                        await _context.SaveChangesAsync();

                        return RedirectToAction(nameof(Index));
                    }
                }
            }

            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID", orderItem.OrderID);
            ViewData["ProductID"] = new SelectList(_context.Products, "ProductID", "Name", orderItem.ProductID);

            return View(orderItem);
        }

        // GET: OrderItems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderItem = await _context.OrderItems.FindAsync(id);
            if (orderItem == null)
            {
                return NotFound();
            }
            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID", orderItem.OrderID);
            ViewData["ProductID"] = new SelectList(_context.Products, "ProductID", "Name", orderItem.ProductID);
            return View(orderItem);
        }

        // POST: OrderItems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderItemID,OrderID,ProductID,ProductSizeID,Size,Quantity,UnitPrice")] OrderItem orderItem)
        {
            if (id != orderItem.OrderItemID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the original order item to compare quantities
                    var originalOrderItem = await _context.OrderItems.AsNoTracking().FirstOrDefaultAsync(o => o.OrderItemID == id);

                    if (originalOrderItem == null)
                    {
                        return NotFound();
                    }

                    int quantityDifference = orderItem.Quantity - originalOrderItem.Quantity;

                    // If product has sizes, update ProductSize stock
                    if (orderItem.ProductSizeID.HasValue)
                    {
                        var productSize = await _context.ProductSizes.FindAsync(orderItem.ProductSizeID.Value);
                        if (productSize != null)
                        {
                            if (productSize.StockQuantity < quantityDifference)
                            {
                                ModelState.AddModelError("", "Not enough stock for this change.");
                                ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID", orderItem.OrderID);
                                ViewData["ProductID"] = new SelectList(_context.Products, "ProductID", "Name", orderItem.ProductID);
                                return View(orderItem);
                            }
                            productSize.StockQuantity -= quantityDifference;
                            _context.Update(productSize);
                        }
                    }
                    else
                    {
                        // Update product stock if no ProductSize
                        var product = await _context.Products.FindAsync(orderItem.ProductID);
                        if (product != null)
                        {
                            if (product.StockQuantity < quantityDifference)
                            {
                                ModelState.AddModelError("", "Not enough stock for this change.");
                                ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID", orderItem.OrderID);
                                ViewData["ProductID"] = new SelectList(_context.Products, "ProductID", "Name", orderItem.ProductID);
                                return View(orderItem);
                            }
                            product.StockQuantity -= quantityDifference;
                            _context.Update(product);
                        }
                    }

                    _context.Update(orderItem);
                    await _context.SaveChangesAsync();

                    // Update product stock quantity
                    await UpdateProductStockQuantity(orderItem.ProductID);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderItemExists(orderItem.OrderItemID))
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
            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID", orderItem.OrderID);
            ViewData["ProductID"] = new SelectList(_context.Products, "ProductID", "Name", orderItem.ProductID);
            return View(orderItem);
        }

        // GET: OrderItems/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderItem = await _context.OrderItems
                .Include(o => o.Order)
                .Include(o => o.Product)
                .FirstOrDefaultAsync(m => m.OrderItemID == id);
            if (orderItem == null)
            {
                return NotFound();
            }

            return View(orderItem);
        }

        // POST: OrderItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var orderItem = await _context.OrderItems.FindAsync(id);
            if (orderItem != null)
            {
                // Return stock to ProductSize or Product
                if (orderItem.ProductSizeID.HasValue)
                {
                    var productSize = await _context.ProductSizes.FindAsync(orderItem.ProductSizeID.Value);
                    if (productSize != null)
                    {
                        productSize.StockQuantity += orderItem.Quantity;
                        _context.Update(productSize);
                    }
                }
                else
                {
                    var product = await _context.Products.FindAsync(orderItem.ProductID);
                    if (product != null)
                    {
                        product.StockQuantity = (product.StockQuantity ?? 0) + orderItem.Quantity;
                        _context.Update(product);
                    }
                }

                _context.OrderItems.Remove(orderItem);
                await _context.SaveChangesAsync();

                // Update product stock quantity
                await UpdateProductStockQuantity(orderItem.ProductID);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool OrderItemExists(int id)
        {
            return _context.OrderItems.Any(e => e.OrderItemID == id);
        }

        private async Task UpdateProductStockQuantity(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null && product.HasSizes)
            {
                var totalStockInSizes = await _context.ProductSizes
                    .Where(ps => ps.ProductID == productId)
                    .SumAsync(ps => ps.StockQuantity);

                product.StockQuantity = totalStockInSizes;
                _context.Update(product);
                await _context.SaveChangesAsync();
            }
        }
    }
}
