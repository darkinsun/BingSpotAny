using System;
using SkiaSharp;

namespace BingSpotAny
{
    public static class ImageWatermarkHelper
    {
        public static byte[] ApplyWatermark(byte[] originalImageBytes, string text, WallpaperSettings settings)
        {
            if (string.IsNullOrWhiteSpace(text))
                return originalImageBytes;

            try
            {
                using var bitmap = SKBitmap.Decode(originalImageBytes);
                using var canvas = new SKCanvas(bitmap);

                // DEFAULT COLOR
                SKColor textColor = SKColors.White;

                // BUG FIX: Prevent TryParse from overwriting our default color with Transparent if it fails.
                if (SKColor.TryParse(settings.WatermarkColor, out SKColor parsedColor))
                {
                    textColor = parsedColor;
                }
                else
                {
                    string colorStr = settings.WatermarkColor?.ToLowerInvariant() ?? "";
                    if (colorStr == "black") textColor = SKColors.Black;
                    else if (colorStr == "red") textColor = SKColors.Red;
                }

                // 1. Font and Size Setup
                using var typeface = SKTypeface.FromFamilyName(settings.WatermarkFontFamily) ?? SKTypeface.Default;
                using var font = new SKFont(typeface, settings.WatermarkFontSize);

                // 2. Main Text Paint
                using var paint = new SKPaint
                {
                    Color = textColor,
                    IsAntialias = true
                };

                // 3. Shadow Paint (Siyah kutu yerine zarif bir gölge efekti için)
                using var shadowPaint = new SKPaint
                {
                    Color = new SKColor(0, 0, 0, 200), // Yarı saydam siyah gölge
                    IsAntialias = true
                };

                // Measure text bounds
                var textBounds = new SKRect();
                font.MeasureText(text, out textBounds, paint);

                float margin = 20f;
                float padding = 10f; // Sadece koordinat hesaplaması için bıraktık

                float x = margin;
                float y = margin;

                // Calculate Position
                switch (settings.WatermarkPosition)
                {
                    case "TopLeft":
                        x = margin + padding;
                        y = margin + textBounds.Height + padding;
                        break;
                    case "TopRight":
                        x = bitmap.Width - textBounds.Width - margin - padding;
                        y = margin + textBounds.Height + padding;
                        break;
                    case "BottomRight":
                        x = bitmap.Width - textBounds.Width - margin - padding;
                        y = bitmap.Height - margin - padding;
                        break;
                    case "BottomLeft":
                    default:
                        x = margin + padding;
                        y = bitmap.Height - margin - padding;
                        break;
                }


                // 4. Önce Gölgeyi Çiz (Sağa ve aşağıya 2 piksel kaydırarak)
                canvas.DrawText(text, x + 2, y + 2, font, shadowPaint);

                // 5. Ana Metni Tam Üzerine Çiz
                canvas.DrawText(text, x, y, font, paint);

                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 100);
                
                return data.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Watermark process has failed: {ex.Message}");
                return originalImageBytes;
            }
        }
    }
}