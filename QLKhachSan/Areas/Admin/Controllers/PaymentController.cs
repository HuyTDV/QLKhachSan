using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLKhachSan.Models;
using System.Security.Cryptography;
using System.Text;

namespace QLKhachSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PaymentController : Controller
    {
        private readonly Hotel01Context _context;
        private readonly IConfiguration _configuration;

        public PaymentController(Hotel01Context context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: Admin/Payment
        public async Task<IActionResult> Index(string searchTerm, string paymentMethod, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Room)
                .Include(p => p.Promotion) // THÊM: Load thông tin promotion
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p =>
                    p.TransactionCode.Contains(searchTerm) ||
                    p.Booking.User.FullName.Contains(searchTerm) ||
                    p.PaymentId.ToString().Contains(searchTerm));
            }

            // Payment method filter
            if (!string.IsNullOrEmpty(paymentMethod))
            {
                query = query.Where(p => p.PaymentMethod == paymentMethod);
            }

            // Date range filter
            if (fromDate.HasValue)
            {
                query = query.Where(p => p.PaidAt >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                query = query.Where(p => p.PaidAt <= toDate.Value);
            }

            var payments = await query
                .OrderByDescending(p => p.PaidAt)
                .ToListAsync();

            // Pass filter values to view
            ViewBag.SearchTerm = searchTerm;
            ViewBag.PaymentMethod = paymentMethod;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(payments);
        }

        // GET: Admin/Payment/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy thanh toán!";
                return RedirectToAction(nameof(Index));
            }

            var payment = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Room)
                        .ThenInclude(r => r.HotelBranch)
                .Include(p => p.Promotion) // THÊM: Load promotion
                .FirstOrDefaultAsync(m => m.PaymentId == id);

            if (payment == null)
            {
                TempData["Error"] = "Thanh toán không tồn tại!";
                return RedirectToAction(nameof(Index));
            }

            return View(payment);
        }

        // GET: Admin/Payment/Create
        public async Task<IActionResult> Create(int? bookingId)
        {
            // Get pending bookings that don't have payment yet
            var bookingsWithoutPayment = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .Where(b => !_context.Payments.Any(p => p.BookingId == b.BookingId))
                .Select(b => new
                {
                    b.BookingId,
                    DisplayText = $"#{b.BookingId} - {b.User.FullName} - Phòng {b.Room.RoomNumber} - {b.TotalPrice:N0}đ"
                })
                .ToListAsync();

            ViewData["BookingId"] = new SelectList(bookingsWithoutPayment, "BookingId", "DisplayText", bookingId);

            // If bookingId is provided, get booking details
            if (bookingId.HasValue)
            {
                var booking = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                if (booking != null)
                {
                    ViewBag.BookingDetails = booking;
                }
            }

            return View();
        }

        // POST: Admin/Payment/Create
        // ===== ĐÃ SỬA: THÊM CÁC THAM SỐ PROMOTION =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("PaymentId,BookingId,Amount,PaymentMethod,TransactionCode,Notes,PromotionId,DiscountAmount,PromotionCode")] Payment payment,
            string paymentType = "direct")
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if booking exists
                    var booking = await _context.Bookings.FindAsync(payment.BookingId);
                    if (booking == null)
                    {
                        ModelState.AddModelError("BookingId", "Booking không tồn tại!");
                        await LoadCreateViewData(payment.BookingId);
                        return View(payment);
                    }

                    // Check if payment already exists for this booking
                    var existingPayment = await _context.Payments
                        .FirstOrDefaultAsync(p => p.BookingId == payment.BookingId);
                    if (existingPayment != null)
                    {
                        ModelState.AddModelError("BookingId", "Booking này đã có thanh toán!");
                        await LoadCreateViewData(payment.BookingId);
                        return View(payment);
                    }

                    // Validate amount
                    if (payment.Amount <= 0)
                    {
                        ModelState.AddModelError("Amount", "Số tiền phải lớn hơn 0!");
                        await LoadCreateViewData(payment.BookingId);
                        return View(payment);
                    }

                    // ===== THÊM MỚI: VALIDATE PROMOTION =====
                    // ===== VALIDATE PROMOTION (ĐƠN GIẢN HÓA) =====
                    if (payment.PromotionId.HasValue && payment.PromotionId > 0)
                    {
                        var promotion = await _context.Promotions.FindAsync(payment.PromotionId);
                        if (promotion == null)
                        {
                            ModelState.AddModelError("", "Mã giảm giá không hợp lệ!");
                            await LoadCreateViewData(payment.BookingId);
                            return View(payment);
                        }

                        // Check date range (BỎ CHECK IsActive)
                        var today = DateOnly.FromDateTime(DateTime.Now);
                        if (promotion.StartDate.HasValue && promotion.StartDate > today)
                        {
                            ModelState.AddModelError("", "Mã giảm giá chưa có hiệu lực!");
                            await LoadCreateViewData(payment.BookingId);
                            return View(payment);
                        }
                        if (promotion.EndDate.HasValue && promotion.EndDate < today)
                        {
                            ModelState.AddModelError("", "Mã giảm giá đã hết hạn!");
                            await LoadCreateViewData(payment.BookingId);
                            return View(payment);
                        }
                    }
                    // ===========================================
                    // ========================================

                    // If using VNPay, redirect to VNPay payment
                    if (paymentType == "vnpay")
                    {
                        // Store promotion info in TempData for VNPay callback
                        if (payment.PromotionId.HasValue)
                        {
                            TempData["VNPay_PromotionId"] = payment.PromotionId.Value;
                            TempData["VNPay_DiscountAmount"] = payment.DiscountAmount?.ToString() ?? "0"; // ← SỬA
                            TempData["VNPay_PromotionCode"] = payment.PromotionCode;
                        }
                        return RedirectToAction("CreatePaymentVNPay", new { bookingId = payment.BookingId, amount = payment.Amount });
                    }

                    // Direct payment
                    payment.PaidAt = DateTime.Now;

                    // Generate transaction code if empty
                    if (string.IsNullOrEmpty(payment.TransactionCode))
                    {
                        payment.TransactionCode = $"TXN{DateTime.Now:yyyyMMddHHmmss}";
                    }

                    _context.Add(payment);

                    // Update booking status
                    booking.Status = "Confirmed";
                    _context.Update(booking);

                    await _context.SaveChangesAsync();

                    // ===== SỬA: THÔNG BÁO SUCCESS CÓ PROMOTION =====
                    TempData["Success"] = payment.PromotionId.HasValue
                        ? $"Thêm thanh toán thành công! Đã áp dụng mã giảm giá {payment.PromotionCode} (- {payment.DiscountAmount:N0}₫)"
                        : "Thêm thanh toán thành công!";
                    // ===============================================

                    return RedirectToAction(nameof(Details), new { id = payment.PaymentId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                }
            }

            await LoadCreateViewData(payment.BookingId);
            return View(payment);
        }

        // ===== THÊM MỚI: API VALIDATE PROMOTION =====
        // POST: Validate Promotion Code
        [HttpPost]
        public async Task<IActionResult> ValidatePromotion(string promotionCode, int bookingId, decimal amount)
        {
            try
            {
                // Tìm mã giảm giá trong database (BỎ CHECK IsActive)
                var promotion = await _context.Promotions
                    .FirstOrDefaultAsync(p => p.Code == promotionCode);

                if (promotion == null)
                {
                    return Json(new { success = false, message = "Mã giảm giá không tồn tại!" });
                }

                // Kiểm tra ngày hiệu lực
                var today = DateOnly.FromDateTime(DateTime.Now);

                if (promotion.StartDate.HasValue && promotion.StartDate > today)
                {
                    return Json(new { success = false, message = "Mã giảm giá chưa có hiệu lực!" });
                }

                if (promotion.EndDate.HasValue && promotion.EndDate < today)
                {
                    return Json(new { success = false, message = "Mã giảm giá đã hết hạn!" });
                }

                // Kiểm tra xem mã này có discount percent không
                if (!promotion.DiscountPercent.HasValue || promotion.DiscountPercent <= 0)
                {
                    return Json(new { success = false, message = "Mã giảm giá không hợp lệ!" });
                }

                // Tính số tiền giảm giá (theo phần trăm)
                decimal discountAmount = amount * (promotion.DiscountPercent.Value / 100);

                // Không được giảm quá tổng tiền
                if (discountAmount > amount)
                {
                    discountAmount = amount;
                }

                // Làm tròn đến hàng nghìn
                discountAmount = Math.Round(discountAmount / 1000) * 1000;

                return Json(new
                {
                    success = true,
                    promotionId = promotion.PromotionId,
                    discountAmount = discountAmount,
                    discountType = "Percentage",
                    discountValue = promotion.DiscountPercent.Value,
                    message = "Áp dụng mã giảm giá thành công!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
        // ===========================================

        // ===== THAY THẾ METHOD ValidatePromotion TRONG PaymentController.cs =====
        // Tìm method ValidatePromotion (hoặc thêm mới nếu chưa có)
        // Đặt vị trí: SAU method Create và TRƯỚC method CreatePaymentVNPay




        // ===== CẬP NHẬT METHOD Create [HttpPost] =====



        // GET: Create VNPay Payment
        public async Task<IActionResult> CreatePaymentVNPay(int bookingId, decimal amount)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                TempData["Error"] = "Không tìm thấy booking!";
                return RedirectToAction(nameof(Create));
            }

            // VNPay configuration
            string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            string vnp_TmnCode = "YOUR_TMN_CODE";
            string vnp_HashSecret = "YOUR_HASH_SECRET";
            string vnp_ReturnUrl = Url.Action("PaymentCallback", "Payment", new { area = "Admin" }, Request.Scheme);

            // Build VNPay payment URL
            var vnpay = new VNPayLibrary();
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", ((long)(amount * 100)).ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", GetIpAddress());
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan booking #{bookingId}");
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);
            vnpay.AddRequestData("vnp_TxnRef", DateTime.Now.Ticks.ToString());

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

            // Store booking info in TempData for callback
            TempData["VNPay_BookingId"] = bookingId;
            TempData["VNPay_Amount"] = amount.ToString(); // ← SỬA: Chuyển sang String

            return Redirect(paymentUrl);
        }

        // GET: Payment Callback from VNPay
        public async Task<IActionResult> PaymentCallback()
        {
            var vnpay = new VNPayLibrary();
            foreach (string s in Request.Query.Keys)
            {
                if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(s, Request.Query[s]);
                }
            }

            string vnp_HashSecret = "YOUR_HASH_SECRET";
            string vnp_SecureHash = Request.Query["vnp_SecureHash"];
            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

            if (checkSignature)
            {
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                string vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");
                string vnp_Amount = vnpay.GetResponseData("vnp_Amount");
                string vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");

                if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                {
                    // Payment success
                    int bookingId = (int)TempData["VNPay_BookingId"];
                    decimal amount = decimal.Parse(TempData["VNPay_Amount"]?.ToString() ?? "0"); // ← SỬA

                    var payment = new Payment
                    {
                        BookingId = bookingId,
                        Amount = amount,
                        PaymentMethod = "VNPay",
                        TransactionCode = vnp_TxnRef,
                        PaidAt = DateTime.Now,
                        Notes = $"Thanh toán qua VNPay. {vnp_OrderInfo}"
                    };

                    // ===== THÊM: RESTORE PROMOTION INFO =====
                    if (TempData["VNPay_PromotionId"] != null)
                    {
                        payment.PromotionId = (int)TempData["VNPay_PromotionId"];
                        payment.DiscountAmount = decimal.Parse(TempData["VNPay_DiscountAmount"]?.ToString() ?? "0"); // ← SỬA
                        payment.PromotionCode = TempData["VNPay_PromotionCode"]?.ToString();
                    }
                    // ========================================

                    _context.Add(payment);

                    // Update booking status
                    var booking = await _context.Bookings.FindAsync(bookingId);
                    if (booking != null)
                    {
                        booking.Status = "Confirmed";
                        _context.Update(booking);
                    }

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Thanh toán qua VNPay thành công!";
                    return RedirectToAction(nameof(Details), new { id = payment.PaymentId });
                }
                else
                {
                    TempData["Error"] = "Thanh toán thất bại! Mã lỗi: " + vnp_ResponseCode;
                    return RedirectToAction(nameof(Create));
                }
            }
            else
            {
                TempData["Error"] = "Chữ ký không hợp lệ!";
                return RedirectToAction(nameof(Create));
            }
        }

        // GET: Admin/Payment/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy thanh toán!";
                return RedirectToAction(nameof(Index));
            }

            var payment = await _context.Payments
                .Include(p => p.Booking)
                .Include(p => p.Promotion) // THÊM
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
            {
                TempData["Error"] = "Thanh toán không tồn tại!";
                return RedirectToAction(nameof(Index));
            }

            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .Select(b => new
                {
                    b.BookingId,
                    DisplayText = $"#{b.BookingId} - {b.User.FullName} - Phòng {b.Room.RoomNumber}"
                })
                .ToListAsync();

            ViewData["BookingId"] = new SelectList(bookings, "BookingId", "DisplayText", payment.BookingId);
            return View(payment);
        }

        // POST: Admin/Payment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PaymentId,BookingId,Amount,PaymentMethod,TransactionCode,PaidAt,Notes,PromotionId,DiscountAmount,PromotionCode")] Payment payment)
        {
            if (id != payment.PaymentId)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ!";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(payment);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật thanh toán thành công!";
                    return RedirectToAction(nameof(Details), new { id = payment.PaymentId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentExists(payment.PaymentId))
                    {
                        TempData["Error"] = "Thanh toán không tồn tại!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                }
            }

            await LoadCreateViewData(payment.BookingId);
            return View(payment);
        }

        // GET: Admin/Payment/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy thanh toán!";
                return RedirectToAction(nameof(Index));
            }

            var payment = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Room)
                .Include(p => p.Promotion) // THÊM
                .FirstOrDefaultAsync(m => m.PaymentId == id);

            if (payment == null)
            {
                TempData["Error"] = "Thanh toán không tồn tại!";
                return RedirectToAction(nameof(Index));
            }

            return View(payment);
        }

        // POST: Admin/Payment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var payment = await _context.Payments.FindAsync(id);
                if (payment != null)
                {
                    _context.Payments.Remove(payment);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Xóa thanh toán thành công!";
                }
                else
                {
                    TempData["Error"] = "Thanh toán không tồn tại!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi xóa: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.PaymentId == id);
        }

        private async Task LoadCreateViewData(int? bookingId)
        {
            var bookingsWithoutPayment = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .Where(b => !_context.Payments.Any(p => p.BookingId == b.BookingId))
                .Select(b => new
                {
                    b.BookingId,
                    DisplayText = $"#{b.BookingId} - {b.User.FullName} - Phòng {b.Room.RoomNumber} - {b.TotalPrice:N0}đ"
                })
                .ToListAsync();

            ViewData["BookingId"] = new SelectList(bookingsWithoutPayment, "BookingId", "DisplayText", bookingId);
        }

        private string GetIpAddress()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1")
            {
                ipAddress = "127.0.0.1";
            }
            return ipAddress;
        }
    }



    // VNPay Library Helper
    public class VNPayLibrary
    {
        private SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
        private SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out string value) ? value : string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
        {
            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in _requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value) + "&");
                }
            }

            string queryString = data.ToString();
            if (queryString.Length > 0)
            {
                queryString = queryString.Remove(queryString.Length - 1, 1);
            }

            string signData = queryString;
            string vnp_SecureHash = HmacSHA512(vnp_HashSecret, signData);

            return baseUrl + "?" + queryString + "&vnp_SecureHash=" + vnp_SecureHash;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in _responseData)
            {
                if (!string.IsNullOrEmpty(kv.Value) && kv.Key != "vnp_SecureHash")
                {
                    data.Append(Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value) + "&");
                }
            }

            string signData = data.ToString();
            if (signData.Length > 0)
            {
                signData = signData.Remove(signData.Length - 1, 1);
            }

            string myChecksum = HmacSHA512(secretKey, signData);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString();
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return string.CompareOrdinal(x, y);
        }
    }
}