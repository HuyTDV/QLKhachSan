using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLKhachSan.Models;

namespace QLKhachSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BlogPostController : Controller
    {
        private readonly Hotel01Context _context;

        public BlogPostController(Hotel01Context context)
        {
            _context = context;
        }

        // GET: Admin/BlogPost - CÓ PHÂN TRANG
        public async Task<IActionResult> Index(
            int? authorId,
            string searchTerm,
            int pageNumber = 1,
            int pageSize = 10)
        {
            // Validate pageSize
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var query = _context.BlogPosts
                .Include(b => b.Author)
                .AsQueryable();

            // Apply filters
            if (authorId.HasValue)
                query = query.Where(b => b.AuthorId == authorId.Value);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(b =>
                    (b.Title != null && b.Title.Contains(searchTerm)) ||
                    (b.Content != null && b.Content.Contains(searchTerm)));
            }

            // Get total count before pagination
            var totalItems = await query.CountAsync();

            // Apply sorting and pagination
            var posts = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Calculate pagination info
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Pass data to view
            ViewBag.Authors = await _context.Users.Where(u => u.Role == "Manager" || u.Role == "Admin").ToListAsync();
            ViewBag.AuthorId = authorId;
            ViewBag.SearchTerm = searchTerm;

            // Pagination info
            ViewBag.CurrentPage = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            return View(posts);
        }

        // GET: Admin/BlogPost/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.BlogPosts
                .Include(b => b.Author)
                .FirstOrDefaultAsync(m => m.PostId == id);

            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // GET: Admin/BlogPost/Create
        public IActionResult Create()
        {
            ViewData["AuthorId"] = new SelectList(_context.Users, "UserId", "FullName");
            return View();
        }

        // POST: Admin/BlogPost/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PostId,Title,Content,AuthorId")] BlogPost post)
        {
            if (ModelState.IsValid)
            {
                post.CreatedAt = DateTime.Now;
                _context.Add(post);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm bài viết thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["AuthorId"] = new SelectList(_context.Users, "UserId", "FullName", post.AuthorId);
            return View(post);
        }

        // GET: Admin/BlogPost/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }
            ViewData["AuthorId"] = new SelectList(_context.Users, "UserId", "FullName", post.AuthorId);
            return View(post);
        }

        // POST: Admin/BlogPost/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PostId,Title,Content,AuthorId,CreatedAt")] BlogPost post)
        {
            if (id != post.PostId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(post);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật bài viết thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PostExists(post.PostId))
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
            ViewData["AuthorId"] = new SelectList(_context.Users, "UserId", "FullName", post.AuthorId);
            return View(post);
        }

        // GET: Admin/BlogPost/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.BlogPosts
                .Include(b => b.Author)
                .FirstOrDefaultAsync(m => m.PostId == id);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // POST: Admin/BlogPost/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post != null)
            {
                _context.BlogPosts.Remove(post);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa bài viết thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PostExists(int id)
        {
            return _context.BlogPosts.Any(e => e.PostId == id);
        }
    }
}