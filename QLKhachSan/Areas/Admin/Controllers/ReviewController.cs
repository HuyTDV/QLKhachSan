using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLKhachSan.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font;
using iText.IO.Font.Constants;

namespace QLKhachSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReviewController : Controller
    {
        private readonly Hotel01Context _context;

        public ReviewController(Hotel01Context context)
        {
            _context = context;
        }

        // GET: Admin/Review - Với Filter + Pagination
        public async Task<IActionResult> Index(
            int? branchId,
            int? rating,
            string replyStatus, // "replied", "not_replied", "all"
            int? roomId,
            string searchTerm,
            string sortBy = "date_desc",
            int pageNumber = 1,
            int pageSize = 10)
        {
            // Validate pageSize
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var query = _context.Reviews
                .Include(r => r.Room)
                    .ThenInclude(room => room.Branch)
                .Include(r => r.User)
                .AsQueryable();

            // Apply filters
            if (branchId.HasValue)
                query = query.Where(r => r.Room.BranchId == branchId.Value);

            if (rating.HasValue)
                query = query.Where(r => r.Rating == rating.Value);

            if (!string.IsNullOrEmpty(replyStatus))
            {
                if (replyStatus == "replied")
                    query = query.Where(r => !string.IsNullOrEmpty(r.Reply));
                else if (replyStatus == "not_replied")
                    query = query.Where(r => string.IsNullOrEmpty(r.Reply));
            }

            if (roomId.HasValue)
                query = query.Where(r => r.RoomId == roomId.Value);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(r =>
                    (r.User != null && r.User.FullName != null && r.User.FullName.Contains(searchTerm)) ||
                    (r.Comment != null && r.Comment.Contains(searchTerm)) ||
                    (r.Reply != null && r.Reply.Contains(searchTerm)));
            }

            // Apply sorting
            query = sortBy switch
            {
                "date_asc" => query.OrderBy(r => r.CreatedAt),
                "date_desc" => query.OrderByDescending(r => r.CreatedAt),
                "rating_asc" => query.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAt),
                "rating_desc" => query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt),
                "customer_asc" => query.OrderBy(r => r.User.FullName),
                "customer_desc" => query.OrderByDescending(r => r.User.FullName),
                _ => query.OrderByDescending(r => r.CreatedAt)
            };

            // Get total count before pagination
            var totalItems = await query.CountAsync();

            // Apply pagination
            var reviews = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Calculate pagination info
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Calculate statistics (on filtered data, not paginated)
            var allFilteredReviews = await _context.Reviews
                .Include(r => r.Room)
                    .ThenInclude(room => room.Branch)
                .Include(r => r.User)
                .Where(r => query.Select(q => q.ReviewId).Contains(r.ReviewId))
                .ToListAsync();

            // Tính toán và gán trực tiếp vào ViewBag (không dùng var stats = new { })
            ViewBag.TotalReviews = allFilteredReviews.Count;
            ViewBag.AverageRating = allFilteredReviews.Any() ? allFilteredReviews.Average(r => r.Rating ?? 0) : 0;
            ViewBag.RepliedCount = allFilteredReviews.Count(r => !string.IsNullOrEmpty(r.Reply));
            ViewBag.NotRepliedCount = allFilteredReviews.Count(r => string.IsNullOrEmpty(r.Reply));
            ViewBag.FiveStarCount = allFilteredReviews.Count(r => r.Rating == 5);
            ViewBag.FourStarCount = allFilteredReviews.Count(r => r.Rating == 4);
            ViewBag.ThreeStarCount = allFilteredReviews.Count(r => r.Rating == 3);
            ViewBag.TwoStarCount = allFilteredReviews.Count(r => r.Rating == 2);
            ViewBag.OneStarCount = allFilteredReviews.Count(r => r.Rating == 1);

            // Pass data to view
            ViewBag.Branches = await _context.HotelBranches.ToListAsync();
            ViewBag.Rooms = await _context.Rooms.Select(r => new { r.RoomId, r.RoomNumber }).ToListAsync();
            ViewBag.BranchId = branchId;
            ViewBag.Rating = rating;
            ViewBag.ReplyStatus = replyStatus;
            ViewBag.RoomId = roomId;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SortBy = sortBy;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;
            

            return View(reviews);
        }

        // Quick Reply AJAX
        [HttpPost]
        public async Task<IActionResult> QuickReply(int reviewId, string reply)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null)
                return Json(new { success = false, message = "Không tìm thấy đánh giá" });

            review.Reply = reply;
            _context.Update(review);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Phản hồi thành công!" });
        }

        // Export Excel
        [HttpGet]
        public async Task<IActionResult> ExportExcel(
            int? branchId,
            int? rating,
            string replyStatus,
            int? roomId,
            string searchTerm)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var query = _context.Reviews
                .Include(r => r.Room)
                    .ThenInclude(room => room.Branch)
                .Include(r => r.User)
                .AsQueryable();

            // Apply same filters
            if (branchId.HasValue)
                query = query.Where(r => r.Room.BranchId == branchId.Value);

            if (rating.HasValue)
                query = query.Where(r => r.Rating == rating.Value);

            if (!string.IsNullOrEmpty(replyStatus))
            {
                if (replyStatus == "replied")
                    query = query.Where(r => !string.IsNullOrEmpty(r.Reply));
                else if (replyStatus == "not_replied")
                    query = query.Where(r => string.IsNullOrEmpty(r.Reply));
            }

            if (roomId.HasValue)
                query = query.Where(r => r.RoomId == roomId.Value);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(r =>
                    (r.User != null && r.User.FullName != null && r.User.FullName.Contains(searchTerm)) ||
                    (r.Comment != null && r.Comment.Contains(searchTerm)));
            }

            var reviews = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Danh sách đánh giá");

                // Title
                worksheet.Cells[1, 1].Value = "BÁO CÁO ĐÁNH GIÁ KHÁCH HÀNG - GRANDORA HOTEL";
                worksheet.Cells[1, 1, 1, 8].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells[2, 1].Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                worksheet.Cells[2, 1, 2, 8].Merge = true;
                worksheet.Cells[2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Statistics
                var avgRating = reviews.Any() ? reviews.Average(r => r.Rating ?? 0) : 0;
                worksheet.Cells[3, 1].Value = $"Tổng: {reviews.Count} đánh giá | Điểm TB: {avgRating:F1}/5.0 | Đã phản hồi: {reviews.Count(r => !string.IsNullOrEmpty(r.Reply))}";
                worksheet.Cells[3, 1, 3, 8].Merge = true;
                worksheet.Cells[3, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Headers
                var headers = new[] { "ID", "Khách hàng", "Phòng", "Chi nhánh", "Đánh giá", "Nhận xét", "Phản hồi", "Ngày tạo" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[5, i + 1].Value = headers[i];
                    worksheet.Cells[5, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[5, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[5, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    worksheet.Cells[5, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                // Data
                int row = 6;
                foreach (var review in reviews)
                {
                    worksheet.Cells[row, 1].Value = review.ReviewId;
                    worksheet.Cells[row, 2].Value = review.User?.FullName ?? "N/A";
                    worksheet.Cells[row, 3].Value = $"Phòng {review.Room?.RoomNumber ?? "N/A"}";
                    worksheet.Cells[row, 4].Value = review.Room?.Branch?.BranchName ?? "N/A";
                    worksheet.Cells[row, 5].Value = $"{review.Rating ?? 0}/5";
                    worksheet.Cells[row, 6].Value = review.Comment;
                    worksheet.Cells[row, 7].Value = string.IsNullOrEmpty(review.Reply) ? "Chưa phản hồi" : review.Reply;
                    worksheet.Cells[row, 8].Value = review.CreatedAt?.ToString("dd/MM/yyyy HH:mm");

                    // Color code by rating
                    if (review.Rating >= 4)
                        worksheet.Cells[row, 5].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                    else if (review.Rating <= 2)
                        worksheet.Cells[row, 5].Style.Fill.BackgroundColor.SetColor(Color.LightCoral);

                    worksheet.Cells[row, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    row++;
                }

                worksheet.Cells.AutoFitColumns();

                var dataRange = worksheet.Cells[5, 1, row - 1, 8];
                dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                var stream = new MemoryStream(package.GetAsByteArray());
                var fileName = $"BaoCaoDanhGia_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // Export PDF
        [HttpGet]
        public async Task<IActionResult> ExportPdf(
            int? branchId,
            int? rating,
            string replyStatus,
            int? roomId,
            string searchTerm)
        {
            var query = _context.Reviews
                .Include(r => r.Room)
                    .ThenInclude(room => room.Branch)
                .Include(r => r.User)
                .AsQueryable();

            // Apply same filters
            if (branchId.HasValue)
                query = query.Where(r => r.Room.BranchId == branchId.Value);

            if (rating.HasValue)
                query = query.Where(r => r.Rating == rating.Value);

            if (!string.IsNullOrEmpty(replyStatus))
            {
                if (replyStatus == "replied")
                    query = query.Where(r => !string.IsNullOrEmpty(r.Reply));
                else if (replyStatus == "not_replied")
                    query = query.Where(r => string.IsNullOrEmpty(r.Reply));
            }

            if (roomId.HasValue)
                query = query.Where(r => r.RoomId == roomId.Value);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(r =>
                    (r.User != null && r.User.FullName != null && r.User.FullName.Contains(searchTerm)) ||
                    (r.Comment != null && r.Comment.Contains(searchTerm)));
            }

            var reviews = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

            using (var stream = new MemoryStream())
            {
                var writer = new PdfWriter(stream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                var fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "Arial.ttf");
                PdfFont font;
                try
                {
                    font = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H);
                }
                catch
                {
                    font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                }

                // Title
                var title = new Paragraph("BAO CAO DANH GIA KHACH HANG - GRANDORA HOTEL")
                    .SetFont(font)
                    .SetFontSize(18)
                    .SetBold()
                    .SetTextAlignment(TextAlignment.CENTER);
                document.Add(title);

                var dateInfo = new Paragraph($"Ngay xuat: {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .SetFont(font)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER);
                document.Add(dateInfo);

                // Statistics
                var avgRating = reviews.Any() ? reviews.Average(r => r.Rating ?? 0) : 0;
                var stats = new Paragraph($"Tong: {reviews.Count} danh gia | Diem TB: {avgRating:F1}/5.0 | Da phan hoi: {reviews.Count(r => !string.IsNullOrEmpty(r.Reply))}")
                    .SetFont(font)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER);
                document.Add(stats);

                document.Add(new Paragraph("\n"));

                // Table
                var table = new Table(new float[] { 1, 2, 1.5f, 2, 1f, 3, 1.5f });
                table.SetWidth(UnitValue.CreatePercentValue(100));

                var headers = new[] { "ID", "Khach hang", "Phong", "Chi nhanh", "Rating", "Nhan xet", "Ngay tao" };
                foreach (var header in headers)
                {
                    table.AddHeaderCell(new Cell()
                        .Add(new Paragraph(header).SetFont(font).SetBold())
                        .SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY)
                        .SetTextAlignment(TextAlignment.CENTER));
                }

                foreach (var review in reviews)
                {
                    table.AddCell(new Cell().Add(new Paragraph(review.ReviewId.ToString()).SetFont(font)));
                    table.AddCell(new Cell().Add(new Paragraph(review.User?.FullName ?? "N/A").SetFont(font)));
                    table.AddCell(new Cell().Add(new Paragraph($"Phong {review.Room?.RoomNumber ?? "N/A"}").SetFont(font)));
                    table.AddCell(new Cell().Add(new Paragraph(review.Room?.Branch?.BranchName ?? "N/A").SetFont(font)));
                    table.AddCell(new Cell().Add(new Paragraph($"{review.Rating ?? 0}/5").SetFont(font)).SetTextAlignment(TextAlignment.CENTER));

                    var comment = review.Comment ?? "";
                    if (comment.Length > 50) comment = comment.Substring(0, 50) + "...";
                    table.AddCell(new Cell().Add(new Paragraph(comment).SetFont(font)));

                    table.AddCell(new Cell().Add(new Paragraph(review.CreatedAt?.ToString("dd/MM/yyyy") ?? "N/A").SetFont(font)));
                }

                document.Add(table);
                document.Close();

                var fileName = $"BaoCaoDanhGia_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            }
        }

        // Other existing methods
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var review = await _context.Reviews
                .Include(r => r.Room)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.ReviewId == id);

            if (review == null)
                return NotFound();

            return View(review);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var review = await _context.Reviews
                .Include(r => r.Room)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ReviewId == id);

            if (review == null)
                return NotFound();

            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "RoomNumber", review.RoomId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", review.UserId);
            return View(review);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ReviewId,UserId,RoomId,Rating,Comment,Reply,CreatedAt")] Review review)
        {
            if (id != review.ReviewId)
                return NotFound();

            // Remove validation errors for navigation properties
            ModelState.Remove("User");
            ModelState.Remove("Room");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(review);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật đánh giá thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReviewExists(review.ReviewId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "RoomNumber", review.RoomId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", review.UserId);
            return View(review);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var review = await _context.Reviews
                .Include(r => r.Room)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.ReviewId == id);
            if (review == null)
                return NotFound();

            return View(review);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa đánh giá thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ReviewExists(int id)
        {
            return _context.Reviews.Any(e => e.ReviewId == id);
        }
    }
}