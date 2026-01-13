using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

[Authorize]
public class QrController : Controller
{
    public IActionResult Qr(int id)
    {
        // Nội dung QR (nhân viên chỉ cần bookingId)
        var content = $"BOOKING:{id}";

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qr = new QRCode(data);
        using var bitmap = qr.GetGraphic(20);
        using var ms = new MemoryStream();

        bitmap.Save(ms, ImageFormat.Png);

        return File(ms.ToArray(), "image/png");
    }
}
