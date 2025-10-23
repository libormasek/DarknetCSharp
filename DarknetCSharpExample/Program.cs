using DarknetCSharp;
using OpenCvSharp;

var config = new DarknetConfig(
    @"yolov4-tiny.cfg", 
    @"coco.names")
{
    DetectionThreshold = 0.2f,
    GpuIndex = 0
};

using var darknet = new Darknet(config);

string rtspUrl = "rtsp://";

Environment.SetEnvironmentVariable("OPENCV_FFMPEG_CAPTURE_OPTIONS", "rtsp_transport;udp|stimeout;5000000");

using var cap = new VideoCapture(rtspUrl, VideoCaptureAPIs.FFMPEG);
cap.Set(VideoCaptureProperties.BufferSize, 0);
cap.Set(VideoCaptureProperties.Fps, 10);

if (!cap.IsOpened())
    throw new InvalidOperationException("Unable to open RTSP stream.");

using var frame = new Mat();
using var frameResized = new Mat();

var networkDimensions = darknet.NetworkDimensions;
var imageSize = new Size(networkDimensions.Width, networkDimensions.Height);
var chwBuffer = new float[networkDimensions.Width * networkDimensions.Height * networkDimensions.Channels];

Cv2.NamedWindow("Detections", WindowFlags.AutoSize);

while (true)
{
    if (!cap.Read(frame) || frame.Empty())
        continue;

    Cv2.Resize(frame, frameResized, imageSize, interpolation: InterpolationFlags.Nearest);

    // Convert to normalized RGB CHW float[] for Darknet into preallocated buffer
    BgrMatToRgbImage(frameResized, networkDimensions, chwBuffer);

    Detection[] detections = darknet.Predict(chwBuffer);

    // Draw boxes on the frame we display (same size used for inference)
    DrawDetections(frameResized, detections);

    // Show window
    Cv2.ImShow("Detections", frameResized);
    int key = Cv2.WaitKey(1);
    if (key == 27 || key == 'q') // Esc or 'q' to quit
        break;
}

Cv2.DestroyAllWindows();

static unsafe void BgrMatToRgbImage(Mat mat, NetworkDimensions dimensions, float[] buffer)
{
    int w = dimensions.Width, h = dimensions.Height, hw = w * h;
    int rBase = 0, gBase = hw, bBase = 2 * hw;
    float inv255 = 1f / 255f;

    byte* basePtr = mat.DataPointer;
    int step = (int)mat.Step();

    for (int y = 0; y < h; y++)
    {
        byte* row = basePtr + y * step;
        int rowIndex = y * w;
        for (int x = 0; x < w; x++)
        {
            int idx = rowIndex + x;
            int off = x * 3;
            byte b = row[off + 0];
            byte g = row[off + 1];
            byte r = row[off + 2];

            buffer[rBase + idx] = r * inv255;
            buffer[gBase + idx] = g * inv255;
            buffer[bBase + idx] = b * inv255;
        }
    }
}

static void DrawDetections(Mat mat, IEnumerable<Detection> detections)
{
    int w = mat.Width;
    int h = mat.Height;
    int thickness = Math.Max(1, h / 240);
    double fontScale = Math.Max(0.5, h / 720.0);
    int baseline;

    foreach (var p in detections)
    {
        // Convert normalized DarknetBox (center x,y,w,h in [0..1]) to pixel box
        float cx = p.BoundingBox.x * w;
        float cy = p.BoundingBox.y * h;
        float bw = p.BoundingBox.w * w;
        float bh = p.BoundingBox.h * h;

        int x1 = (int)Math.Round(cx - bw / 2f);
        int y1 = (int)Math.Round(cy - bh / 2f);
        int x2 = (int)Math.Round(cx + bw / 2f);
        int y2 = (int)Math.Round(cy + bh / 2f);

        x1 = Math.Clamp(x1, 0, w - 1);
        y1 = Math.Clamp(y1, 0, h - 1);
        x2 = Math.Clamp(x2, 0, w - 1);
        y2 = Math.Clamp(y2, 0, h - 1);
        if (x2 <= x1 || y2 <= y1) continue;

        // Color per class (simple deterministic hash)
        int hash = p.ClassIndex;
        byte r = (byte)(hash & 0xFF);
        byte g = (byte)((hash >> 8) & 0xFF);
        byte b = (byte)((hash >> 16) & 0xFF);
        var color = new Scalar(b, g, r);

        // Draw rectangle
        Cv2.Rectangle(mat, new Point(x1, y1), new Point(x2, y2), color, thickness);

        // Label text
        string label = $"{p.ClassName} {p.Probability:P0}";
        var textSize = Cv2.GetTextSize(label, HersheyFonts.HersheySimplex, fontScale, thickness, out baseline);
        baseline = Math.Max(baseline, 1);

        int textX = x1;
        int textY = Math.Max(y1 - 4, textSize.Height + baseline + 2);

        // Filled background for readability
        var bgTopLeft = new Point(textX, textY - textSize.Height - baseline);
        var bgBottomRight = new Point(textX + textSize.Width + 4, textY + 2);
        Cv2.Rectangle(mat, bgTopLeft, bgBottomRight, new Scalar(0, 0, 0), -1);

        // Text
        Cv2.PutText(mat, label, new Point(textX + 2, textY - baseline), HersheyFonts.HersheySimplex, fontScale, new Scalar(255, 255, 255), thickness, LineTypes.AntiAlias);
    }
}
