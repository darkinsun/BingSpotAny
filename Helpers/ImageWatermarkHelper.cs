/*
 * BEGIN LICENSE
 * Copyright (c) 2026 BingSpotAny Contributors
 * * This program is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License version 3, as published
 * by the Free Software Foundation.
 *
 * This program is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranties of
 * MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR
 * PURPOSE.  See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program.  If not, see <http://www.gnu.org/licenses/>.
 * END LICENSE
 */
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

                // 3. Shadow Paint (For an elegant shadow effect instead of a black box)
                using var shadowPaint = new SKPaint
                {
                    Color = new SKColor(0, 0, 0, 200), // Translucent black shadow
                    IsAntialias = true
                };

                // Measure text bounds
                var textBounds = new SKRect();
                font.MeasureText(text, out textBounds, paint);

                float margin = 20f;
                float padding = 10f; // Left it for coordinate calculation.

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


                // 4. First, draw the shadow (shifting 2 pixels to the right and down).
                canvas.DrawText(text, x + 2, y + 2, font, shadowPaint);

                //  5. Draw a line directly over the main text.
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