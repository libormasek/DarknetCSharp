using DarknetCSharp;
using OpenCvSharp;

var config = new DarknetConfig(@"cfg\yolov4-tiny.cfg")
{
    DetectionThreshold = 0.8f
};

using var darknet = new Darknet(config);

string rtspUrl = "rtsp://";

Environment.SetEnvironmentVariable("OPENCV_FFMPEG_CAPTURE_OPTIONS", "rtsp_transport;udp|stimeout;5000000");

using var cap = new VideoCapture(rtspUrl, VideoCaptureAPIs.FFMPEG);
cap.Set(VideoCaptureProperties.BufferSize, 1);
cap.Set(VideoCaptureProperties.Fps, 10);

if (!cap.IsOpened())
    throw new InvalidOperationException("Unable to open RTSP stream.");

using var frame = new Mat();
using var frameResized = new Mat();

var dims = darknet.NetworkDimensions;
var imageSize = new Size(dims.Width, dims.Height);
var chwBuffer = new float[dims.Width * dims.Height * dims.Channels];

while (true)
{
    if (!cap.Read(frame) || frame.Empty())
        continue;

    // Prefer AREA for downscaling (quality + speed) and LINEAR for upscaling
    var interp = (frame.Width >= dims.Width && frame.Height >= dims.Height)
        ? InterpolationFlags.Area
        : InterpolationFlags.Linear;

    Cv2.Resize(frame, frameResized, imageSize, interpolation: interp);

    // Convert to normalized RGB CHW float[] for Darknet into preallocated buffer
    BgrMatToRgbImageInPlace(frameResized, dims, chwBuffer);

    var predictions = darknet.Predict(chwBuffer);

    foreach (Prediction prediction in predictions)
    {
        Console.WriteLine($"Class: {prediction.ClassName}, Confidence: {prediction.Confidence:P2}");
    }
}

static unsafe void BgrMatToRgbImageInPlace(Mat mat, NetworkDimensions dimensions, float[] buffer)
{
    // Expect 8-bit, 3-channel BGR input already resized to network size
    if (mat.Empty() || mat.Type() != MatType.CV_8UC3)
        throw new ArgumentException("Expected non-empty 8-bit 3-channel BGR image.", nameof(mat));
    if (mat.Width != dimensions.Width || mat.Height != dimensions.Height)
        throw new ArgumentException("Mat size must match network dimensions.", nameof(dimensions));
    if (buffer == null || buffer.Length < dimensions.Width * dimensions.Height * 3)
        throw new ArgumentException("Destination buffer too small.", nameof(buffer));

    int w = dimensions.Width, h = dimensions.Height, hw = w * h;
    int rBase = 0, gBase = hw, bBase = 2 * hw;
    float inv255 = 1f / 255f;

    byte* basePtr = (byte*)mat.DataPointer;
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
