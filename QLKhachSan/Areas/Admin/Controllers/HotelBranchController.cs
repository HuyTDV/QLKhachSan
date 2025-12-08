using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLKhachSan.Models;

namespace QLKhachSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HotelBranchController : Controller
    {
        private readonly Hotel01Context _context;

        public HotelBranchController(Hotel01Context context)
        {
            _context = context;
        }

        // GET: Admin/HotelBranch
        public async Task<IActionResult> Index()
        {
            var branches = await _context.HotelBranches
                .Include(h => h.Hotel)
                .Include(h => h.Manager)
                .ToListAsync();
            return View(branches);
        }

        // GET: Admin/HotelBranch/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var branch = await _context.HotelBranches
                .Include(h => h.Hotel)
                .Include(h => h.Manager)
                .Include(h => h.Rooms)
                .FirstOrDefaultAsync(m => m.BranchId == id);

            if (branch == null)
            {
                return NotFound();
            }

            return View(branch);
        }

        // GET: Admin/HotelBranch/Create
        public IActionResult Create()
        {
            ViewData["HotelId"] = new SelectList(_context.Hotels, "HotelId", "HotelName");
            ViewData["ManagerId"] = new SelectList(_context.Users.Where(u => u.Role == "Manager"), "UserId", "FullName");
            return View();
        }

        // POST: Admin/HotelBranch/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BranchId,HotelId,BranchName,Address,City,Country,ManagerId,Phone")] HotelBranch branch)
        {
            if (ModelState.IsValid)
            {
                _context.Add(branch);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm chi nhánh thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["HotelId"] = new SelectList(_context.Hotels, "HotelId", "HotelName", branch.HotelId);
            ViewData["ManagerId"] = new SelectList(_context.Users, "UserId", "FullName", branch.ManagerId);
            return View(branch);
        }

        // GET: Admin/HotelBranch/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var branch = await _context.HotelBranches.FindAsync(id);
            if (branch == null)
            {
                return NotFound();
            }
            ViewData["HotelId"] = new SelectList(_context.Hotels, "HotelId", "HotelName", branch.HotelId);
            ViewData["ManagerId"] = new SelectList(_context.Users, "UserId", "FullName", branch.ManagerId);
            return View(branch);
        }

        // POST: Admin/HotelBranch/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BranchId,HotelId,BranchName,Address,City,Country,ManagerId,Phone")] HotelBranch branch)
        {
            if (id != branch.BranchId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(branch);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật chi nhánh thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BranchExists(branch.BranchId))
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
            ViewData["HotelId"] = new SelectList(_context.Hotels, "HotelId", "HotelName", branch.HotelId);
            ViewData["ManagerId"] = new SelectList(_context.Users, "UserId", "FullName", branch.ManagerId);
            return View(branch);
        }

        // GET: Admin/HotelBranch/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var branch = await _context.HotelBranches
                .Include(h => h.Hotel)
                .Include(h => h.Manager)
                .FirstOrDefaultAsync(m => m.BranchId == id);
            if (branch == null)
            {
                return NotFound();
            }

            return View(branch);
        }

        // POST: Admin/HotelBranch/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var branch = await _context.HotelBranches.FindAsync(id);
            if (branch != null)
            {
                _context.HotelBranches.Remove(branch);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa chi nhánh thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool BranchExists(int id)
        {
            return _context.HotelBranches.Any(e => e.BranchId == id);
        }
    }
}