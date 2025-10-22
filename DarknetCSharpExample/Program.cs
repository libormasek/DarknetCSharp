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

var networkDimensions = darknet.NetworkDimensions;
var imageSize = new Size(networkDimensions.Width, networkDimensions.Height);
var chwBuffer = new float[networkDimensions.Width * networkDimensions.Height * networkDimensions.Channels];

while (true)
{
    if (!cap.Read(frame) || frame.Empty())
        continue;

    Cv2.Resize(frame, frameResized, imageSize, interpolation: InterpolationFlags.Nearest);

    // Convert to normalized RGB CHW float[] for Darknet into preallocated buffer
    BgrMatToRgbImage(frameResized, networkDimensions, chwBuffer);

    ICollection<Prediction> predictions = darknet.Predict(chwBuffer);

    foreach (Prediction prediction in predictions)
    {
        Console.WriteLine($"Class: {prediction.ClassName}, Confidence: {prediction.Confidence:P2}");
    }
}

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
