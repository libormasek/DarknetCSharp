using DarknetCSharp;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace DarknetCSharpExample;

public static class ImageUtils
{
    public static void RgbToChw(Image<Rgb24> image, NetworkDimensions dimensions, float[] outputChwBuffer)
    {
        int w = dimensions.Width, h = dimensions.Height, hw = w * h;
        int rBase = 0, gBase = hw, bBase = 2 * hw;
        float inv255 = 1f / 255f;

        for (int y = 0; y < h; y++)
        {
            if (!image.DangerousTryGetSinglePixelMemory(out var memory))
                throw new InvalidOperationException("Unable to access pixel memory.");

            var span = memory.Span;
            int rowIndex = y * w;

            for (int x = 0; x < w; x++)
            {
                int idx = rowIndex + x;
                var pixel = span[idx];

                outputChwBuffer[rBase + idx] = pixel.R * inv255;
                outputChwBuffer[gBase + idx] = pixel.G * inv255;
                outputChwBuffer[bBase + idx] = pixel.B * inv255;
            }
        }
    }

    public static void DrawDetectionsOnImage(Image<Rgb24> image, IEnumerable<Detection> detections)
    {
        int w = image.Width;
        int h = image.Height;
        float thickness = Math.Max(2f, h / 240f);
        float fontSize = Math.Max(12f, h / 60f);

        FontFamily fontFamily;
        if (SystemFonts.TryGet("DejaVu Sans", out fontFamily) ||
            SystemFonts.TryGet("Liberation Sans", out fontFamily) ||
            SystemFonts.TryGet("Arial", out fontFamily) ||
            SystemFonts.TryGet("Helvetica", out fontFamily))
        {
            // Successfully found a font
        }
        else
        {
            // Fallback to any available font
            fontFamily = SystemFonts.Families.FirstOrDefault();
        }

        var font = fontFamily.CreateFont(fontSize, FontStyle.Bold);

        image.Mutate(ctx =>
        {
            foreach (var detection in detections)
            {
                // Convert normalized DarknetBox (center x,y,w,h in [0..1]) to pixel box
                float cx = detection.BoundingBox.x * w;
                float cy = detection.BoundingBox.y * h;
                float bw = detection.BoundingBox.w * w;
                float bh = detection.BoundingBox.h * h;

                float x1 = cx - bw / 2f;
                float y1 = cy - bh / 2f;
                float x2 = cx + bw / 2f;
                float y2 = cy + bh / 2f;

                // Clamp to image bounds
                x1 = Math.Clamp(x1, 0, w - 1);
                y1 = Math.Clamp(y1, 0, h - 1);
                x2 = Math.Clamp(x2, 0, w - 1);
                y2 = Math.Clamp(y2, 0, h - 1);

                if (x2 <= x1 || y2 <= y1) continue;
                
                // Draw rectangle
                var rectangle = new RectangleF(x1, y1, x2 - x1, y2 - y1);
                ctx.Draw(Color.Red, thickness, rectangle);

                string label = $"{detection.ClassName} {detection.Probability:P0}";

                var textOptions = new TextOptions(font)
                {
                    Origin = new PointF(x1 + 2, y1 - fontSize - 2)
                };

                var textBounds = TextMeasurer.MeasureSize(label, textOptions);
                var textBackground = new RectangleF(
                    x1,
                    y1 - fontSize - 4,
                    textBounds.Width + 4,
                    fontSize + 4);

                ctx.Fill(Color.Black, textBackground);
                ctx.DrawText(label, font, Color.White, new PointF(x1 + 2, y1 - fontSize - 2));
            }
        });
    }
}