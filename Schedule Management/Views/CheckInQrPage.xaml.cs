using Microsoft.Maui.Controls;
using ZXing;
using ZXing.Common;
using SkiaSharp;
using System.IO;

namespace Schedule_Management.Views;

[QueryProperty(nameof(Data), "data")]
public partial class CheckInQrPage : ContentPage
{
    public string Data { get; set; } = "default";

    public CheckInQrPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Console.WriteLine($"[QR Page] Data received: {Data}");

        // หน้านี้ใช้เพื่อแสดง QR Code เท่านั้น ไม่เรียก API เช็คอิน

        var writer = new ZXing.BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions
            {
                Height = 250,
                Width = 250,
                Margin = 1
            }
        };

        var pixelData = writer.Write(Data);

        // Convert pixel data to SKBitmap
        using var bitmap = new SKBitmap(pixelData.Width, pixelData.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmap.GetPixels(), pixelData.Pixels.Length);

        using var image = SKImage.FromBitmap(bitmap);
        using var encodedData = image.Encode(SKEncodedImageFormat.Png, 100);

        // Create a new stream from encoded data to avoid using disposed stream
        var imageBytes = encodedData.ToArray();
        QrImage.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
    }
}