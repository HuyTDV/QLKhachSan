using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKhachSan.Models;

namespace QLKhachSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PromotionController : Controller
    {
        private readonly Hotel01Context _context;

        public PromotionController(Hotel01Context context)
        {
            _context = context;
        }

        // GET: Admin/Promotion - CÓ PHÂN TRANG
        public async Task<IActionResult> Index(
            string searchTerm,
            bool? isActive,
            int pageNumber = 1,
            int pageSize = 10)
        {
            // Validate pageSize
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var query = _context.Promotions.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p =>
                    (p.Code != null && p.Code.Contains(searchTerm)) ||
                    (p.Description != null && p.Description.Contains(searchTerm)));
            }

            if (isActive.HasValue)
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                if (isActive.Value)
                {
                    query = query.Where(p => p.StartDate <= today && p.EndDate >= today);
                }
                else
                {
                    query = query.Where(p => p.EndDate < today);
                }
            }

            // Get total count before pagination
            var totalItems = await query.CountAsync();

            // Apply sorting and pagination
            var promotions = await query
                .OrderByDescending(p => p.StartDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Calculate pagination info
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Pass data to view
            ViewBag.SearchTerm = searchTerm;
            ViewBag.IsActive = isActive;

            // Pagination info
            ViewBag.CurrentPage = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            return View(promotions);
        }

        // GET: Admin/Promotion/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(m => m.PromotionId == id);
            if (promotion == null)
            {
                return NotFound();
            }

            return View(promotion);
        }

        // GET: Admin/Promotion/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Promotion/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PromotionId,Code,Description,DiscountPercent,StartDate,EndDate")] Promotion promotion)
        {
            if (ModelState.IsValid)
            {
                _context.Add(promotion);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm khuyến mãi thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(promotion);
        }

        // GET: Admin/Promotion/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }
            return View(promotion);
        }

        // POST: Admin/Promotion/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PromotionId,Code,Description,DiscountPercent,StartDate,EndDate")] Promotion promotion)
        {
            if (id != promotion.PromotionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(promotion);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật khuyến mãi thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PromotionExists(promotion.PromotionId))
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
            return View(promotion);
        }

        // Corrected the return type from IActionTask to IActionResult
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(m => m.PromotionId == id);
            if (promotion == null)
            {
                return NotFound();
            }

            return View(promotion);
        }

        // POST: Admin/Promotion/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion != null)
            {
                _context.Promotions.Remove(promotion);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa khuyến mãi thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PromotionExists(int id)
        {
            return _context.Promotions.Any(e => e.PromotionId == id);
        }
    }
}