using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QLKhachSan.Models;
using System.Drawing;

namespace QLKhachSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BookingController : Controller
    {
        private readonly Hotel01Context _context;

        public BookingController(Hotel01Context context)
        {
            _context = context;
        }

        // GET: Admin/Booking - Có Filter + Pagination
        public async Task<IActionResult> Index(
            int? branchId,
            string status,
            DateOnly? fromDate,
            DateOnly? toDate,
            string searchTerm,
            int pageNumber = 1,
            int pageSize = 10)
        {
            // Validate pageSize
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var query = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.Branch)
                .Include(b => b.User)
                .AsQueryable();

            // Apply filters
            if (branchId.HasValue)
                query = query.Where(b => b.Room.BranchId == branchId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.Status == status);

            if (fromDate.HasValue)
                query = query.Where(b => b.CheckIn >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(b => b.CheckIn <= toDate.Value);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(b =>
                    (b.User != null && b.User.FullName != null && b.User.FullName.Contains(searchTerm)) ||
                    (b.Room != null && b.Room.RoomNumber != null && b.Room.RoomNumber.Contains(searchTerm)));
            }

            // Get total count before pagination
            var totalItems = await query.CountAsync();

            // Apply sorting and pagination
            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Calculate pagination info
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Pass data to view
            ViewBag.Branches = await _context.HotelBranches.ToListAsync();
            ViewBag.BranchId = branchId;
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.SearchTerm = searchTerm;

            // Pagination info
            ViewBag.CurrentPage = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            return View(bookings);
        }

        // Export Excel - GIỮ NGUYÊN (không phân trang khi export)
        [HttpGet]
        public async Task<IActionResult> ExportExcel(int? branchId, string status, DateOnly? fromDate, DateOnly? toDate, string searchTerm)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var query = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.Branch)
                .Include(b => b.User)
                .AsQueryable();

            if (branchId.HasValue)
                query = query.Where(b => b.Room.BranchId == branchId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.Status == status);

            if (fromDate.HasValue)
                query = query.Where(b => b.CheckIn >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(b => b.CheckIn <= toDate.Value);

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(b =>
                    (b.User != null && b.User.FullName != null && b.User.FullName.Contains(searchTerm)) ||
                    (b.Room != null && b.Room.RoomNumber != null && b.Room.RoomNumber.Contains(searchTerm)));

            var bookings = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Danh sách đặt phòng");

                worksheet.Cells[1, 1].Value = "DANH SÁCH ĐẶT PHÒNG - GRANDORA HOTEL";
                worksheet.Cells[1, 1, 1, 9].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells[2, 1].Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                worksheet.Cells[2, 1, 2, 9].Merge = true;
                worksheet.Cells[2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                var headers = new[] { "Mã booking", "Khách hàng", "Phòng", "Chi nhánh", "Check-in", "Check-out", "Trạng thái", "Dịch vụ", "Tổng tiền (VNĐ)" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[4, i + 1].Value = headers[i];
                    worksheet.Cells[4, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[4, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[4, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    worksheet.Cells[4, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                int row = 5;
                decimal total = 0;
                foreach (var booking in bookings)
                {
                    worksheet.Cells[row, 1].Value = $"#{booking.BookingId}";
                    worksheet.Cells[row, 2].Value = booking.User?.FullName ?? "N/A";
                    worksheet.Cells[row, 3].Value = $"Phòng {booking.Room?.RoomNumber ?? "N/A"}";
                    worksheet.Cells[row, 4].Value = booking.Room?.Branch?.BranchName ?? "N/A";
                    worksheet.Cells[row, 5].Value = booking.CheckIn?.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 6].Value = booking.CheckOut?.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 7].Value = booking.Status;
                    worksheet.Cells[row, 8].Value = booking.ServicesUsed;
                    worksheet.Cells[row, 9].Value = booking.TotalPrice;
                    worksheet.Cells[row, 9].Style.Numberformat.Format = "#,##0";
                    total += booking.TotalPrice ?? 0;
                    row++;
                }

                worksheet.Cells[row, 8].Value = "TỔNG CỘNG:";
                worksheet.Cells[row, 8].Style.Font.Bold = true;
                worksheet.Cells[row, 9].Value = total;
                worksheet.Cells[row, 9].Style.Font.Bold = true;
                worksheet.Cells[row, 9].Style.Numberformat.Format = "#,##0";
                worksheet.Cells[row, 9].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[row, 9].Style.Fill.BackgroundColor.SetColor(Color.Yellow);

                worksheet.Cells.AutoFitColumns();

                var dataRange = worksheet.Cells[4, 1, row, 9];
                dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                var stream = new MemoryStream(package.GetAsByteArray());
                var fileName = $"DanhSachDatPhong_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // Export PDF - GIỮ NGUYÊN
        [HttpGet]
        public async Task<IActionResult> ExportPdf(int? branchId, string status, DateOnly? fromDate, DateOnly? toDate, string searchTerm)
        {
            var query = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.Branch)
                .Include(b => b.User)
                .AsQueryable();

            if (branchId.HasValue)
                query = query.Where(b => b.Room.BranchId == branchId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.Status == status);

            if (fromDate.HasValue)
                query = query.Where(b => b.CheckIn >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(b => b.CheckIn <= toDate.Value);

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(b =>
                    (b.User != null && b.User.FullName != null && b.User.FullName.Contains(searchTerm)) ||
                    (b.Room != null && b.Room.RoomNumber != null && b.Room.RoomNumber.Contains(searchTerm)));

            var bookings = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();

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

                var title = new Paragraph("DANH SACH DAT PHONG - GRANDORA HOTEL")
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

                document.Add(new Paragraph("\n"));

                var table = new Table(new float[] { 1, 2, 1.5f, 2, 1.5f, 1.5f, 1.5f, 2 });
                table.SetWidth(UnitValue.CreatePercentValue(100));

                var headers = new[] { "Ma", "Khach hang", "Phong", "Chi nhanh", "Check-in", "Check-out", "Trang thai", "Tong tien (VND)" };
                foreach (var header in headers)
                {
                    table.AddHeaderCell(new Cell()
                        .Add(new Paragraph(header).SetFont(font).SetBold())
                        .SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY)
                        .SetTextAlignment(TextAlignment.CENTER));
                }

                decimal total = 0;
                foreach (var booking in bookings)
                {
                    table.AddCell(new Cell().Add(new Paragraph($"#{booking.BookingId}").SetFont(font)));
                    table.AddCell(new Cell().Add(new Paragraph(booking.User?.FullName ?? "N/A").SetFont(font)));
                    table.AddCell(new Cell().Add(new Paragraph($"Phong {booking.Room?.RoomNumber ?? "N/A"}").SetFont(font)));
                    table.AddCell(new Cell().Add(new Paragraph(booking.Room?.Branch?.BranchName ?? "N/A").SetFont(font)));
                    table.AddCell(new Cell().Add(new Paragraph(booking.CheckIn?.ToString("dd/MM/yyyy") ?? "N/A").SetFont(font)));
                    table.AddCell(new Cell().Add(new Paragraph(booking.CheckOut?.ToString("dd/MM/yyyy") ?? "N/A").SetFont(font)));
                    table.AddCell(new Cell().Add(new Paragraph(booking.Status ?? "N/A").SetFont(font)));
                    table.AddCell(new Cell().Add(new Paragraph((booking.TotalPrice ?? 0).ToString("N0")).SetFont(font)).SetTextAlignment(TextAlignment.RIGHT));

                    total += booking.TotalPrice ?? 0;
                }

                table.AddCell(new Cell(1, 7)
                    .Add(new Paragraph("TONG CONG:").SetFont(font).SetBold())
                    .SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Cell()
                    .Add(new Paragraph(total.ToString("N0")).SetFont(font).SetBold())
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetBackgroundColor(iText.Kernel.Colors.ColorConstants.YELLOW));

                document.Add(table);
                document.Close();

                var fileName = $"DanhSachDatPhong_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            }
        }

        // Các method khác giữ nguyên
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.User)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(m => m.BookingId == id);

            if (booking == null)
                return NotFound();

            return View(booking);
        }

        public IActionResult Create()
        {
            LoadViewBagData();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("BookingId,UserId,RoomId,CheckIn,CheckOut,TotalPrice,Status,ServicesUsed")] Booking booking,
            string? newCustomerName,
            string? newCustomerPhone,
            string? newCustomerEmail)
        {
            if (!string.IsNullOrEmpty(newCustomerName) && !string.IsNullOrEmpty(newCustomerPhone))
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Phone == newCustomerPhone);

                if (existingUser != null)
                {
                    booking.UserId = existingUser.UserId;
                    TempData["Info"] = $"Sử dụng tài khoản có sẵn: {existingUser.FullName}";
                }
                else
                {
                    if (!string.IsNullOrEmpty(newCustomerEmail))
                    {
                        var existingEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == newCustomerEmail);
                        if (existingEmail != null)
                        {
                            ModelState.AddModelError("", $"Email {newCustomerEmail} đã tồn tại.");
                            LoadViewBagData(booking.RoomId, booking.UserId);
                            return View(booking);
                        }
                    }

                    var newUser = new User
                    {
                        FullName = newCustomerName,
                        Phone = newCustomerPhone,
                        Email = string.IsNullOrEmpty(newCustomerEmail) ? $"{newCustomerPhone}@walk-in.com" : newCustomerEmail,
                        Username = newCustomerPhone,
                        PasswordHash = "walk-in-customer",
                        Role = "Customer",
                        CreatedAt = DateTime.Now
                    };

                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                    booking.UserId = newUser.UserId;
                    TempData["Info"] = $"Đã tạo tài khoản mới: {newCustomerName}";
                }
            }

            ModelState.Remove("newCustomerName");
            ModelState.Remove("newCustomerPhone");
            ModelState.Remove("newCustomerEmail");
            ModelState.Remove("UserId");
            ModelState.Remove("Room");
            ModelState.Remove("User");

            if (!booking.UserId.HasValue || booking.UserId.Value == 0)
            {
                ModelState.AddModelError("UserId", "Vui lòng chọn khách hàng hoặc nhập thông tin khách mới.");
            }

            if (booking.CheckIn.HasValue && booking.CheckOut.HasValue)
            {
                if (booking.CheckOut <= booking.CheckIn)
                {
                    ModelState.AddModelError("CheckOut", "Ngày trả phòng phải sau ngày nhận phòng.");
                }
                if (booking.CheckIn < DateOnly.FromDateTime(DateTime.Now))
                {
                    ModelState.AddModelError("CheckIn", "Ngày nhận phòng không được là ngày trong quá khứ.");
                }

                var isConflict = await _context.Bookings.AnyAsync(b =>
                    b.RoomId == booking.RoomId &&
                    b.Status != "Cancelled" &&
                    ((booking.CheckIn >= b.CheckIn && booking.CheckIn < b.CheckOut) ||
                     (booking.CheckOut > b.CheckIn && booking.CheckOut <= b.CheckOut)));

                if (isConflict)
                {
                    ModelState.AddModelError("RoomId", "Phòng này đã có người đặt trong khoảng thời gian này.");
                }
            }

            if (ModelState.IsValid)
            {
                booking.CreatedAt = DateTime.Now;
                booking.Status = booking.Status ?? "Confirmed";

                _context.Add(booking);

                var room = await _context.Rooms.FindAsync(booking.RoomId);
                if (room != null)
                {
                    if (booking.CheckIn <= DateOnly.FromDateTime(DateTime.Now))
                    {
                        room.Status = "Booked";
                        _context.Update(room);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đặt phòng thành công!";
                return RedirectToAction(nameof(Create));
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors);
            foreach (var error in errors) Console.WriteLine(">>> LỖI CÒN LẠI: " + error.ErrorMessage);

            LoadViewBagData(booking.RoomId, booking.UserId);
            return View(booking);
        }

        private void LoadViewBagData(int? selectedRoomId = null, int? selectedUserId = null)
        {
            var availableRooms = _context.Rooms
                .Where(r => r.Status == "Available")
                .Select(r => new SelectListItem
                {
                    Value = r.RoomId.ToString(),
                    Text = $"Phòng {r.RoomNumber} - {r.RoomType} - {r.Price:N0}₫/đêm"
                })
                .ToList();

            ViewBag.RoomId = new SelectList(availableRooms, "Value", "Text", selectedRoomId);
            ViewBag.UserId = new SelectList(
                _context.Users.Where(u => u.Role == "Customer"),
                "UserId",
                "FullName",
                selectedUserId
            );

            ViewBag.RoomData = _context.Rooms
                .Where(r => r.Status == "Available")
                .Select(r => new {
                    roomId = r.RoomId,
                    price = r.Price,
                    roomNumber = r.RoomNumber,
                    roomType = r.RoomType
                })
                .ToList();
        }

        [HttpGet]
        public async Task<JsonResult> GetRoomPrice(int roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room != null)
            {
                return Json(new
                {
                    success = true,
                    price = room.Price,
                    roomNumber = room.RoomNumber,
                    roomType = room.RoomType,
                    capacity = room.Capacity,
                    amenities = room.Amenities
                });
            }
            return Json(new
            {
                success = false,
                message = "Không tìm thấy thông tin phòng"
            });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
                return NotFound();

            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "RoomNumber", booking.RoomId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", booking.UserId);
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookingId,UserId,RoomId,CheckIn,CheckOut,TotalPrice,Status,ServicesUsed,CreatedAt")] Booking booking)
        {
            if (id != booking.BookingId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật booking thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.BookingId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "RoomNumber", booking.RoomId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", booking.UserId);
            return View(booking);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.BookingId == id);

            if (booking == null)
                return NotFound();

            return View(booking);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                var room = await _context.Rooms.FindAsync(booking.RoomId);
                if (room != null)
                {
                    room.Status = "Available";
                    _context.Update(room);
                }

                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa booking thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.BookingId == id);
        }
    }
}