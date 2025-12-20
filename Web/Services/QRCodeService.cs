using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace Web.Services;

public interface IQrCodeService
{
    string ToDataUrlPng(string text, int pixelsPerModule = 8);
}

public sealed class QrCodeService : IQrCodeService
{
    public string ToDataUrlPng(string text, int pixelsPerModule = 8)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);

        using var qr = new PngByteQRCode(data);
        var bytes = qr.GetGraphic(pixelsPerModule);

        return "data:image/png;base64," + Convert.ToBase64String(bytes);
    }
}
