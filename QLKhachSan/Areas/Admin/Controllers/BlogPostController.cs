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

        // GET: Admin/BlogPost
        public async Task<IActionResult> Index()
        {
            var posts = await _context.BlogPosts
                .Include(b => b.Author)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
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