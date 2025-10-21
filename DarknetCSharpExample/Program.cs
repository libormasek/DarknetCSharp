using DarknetCSharp;
using OpenCvSharp;

var config = new DarknetConfig(@"cfg\yolov4-tiny.cfg");

using var darknet = new Darknet(config);

string rtspUrl = "rtsp://";

Environment.SetEnvironmentVariable("OPENCV_FFMPEG_CAPTURE_OPTIONS", "rtsp_transport;tcp|stimeout;5000000");

using var cap = new VideoCapture(rtspUrl, VideoCaptureAPIs.FFMPEG);
cap.Set(VideoCaptureProperties.BufferSize, 1);
cap.Set(VideoCaptureProperties.Fps, 10);

if (!cap.IsOpened())
    throw new InvalidOperationException("Unable to open RTSP stream.");

using var frame = new Mat();
using var frameResized = new Mat();

var imageSize = new Size(darknet.NetworkDimensions.Width, darknet.NetworkDimensions.Height);

while (true)
{
    if (!cap.Read(frame) || frame.Empty())
        continue; // or break if you prefer ending on read failure

    Cv2.Resize(frame, frameResized, imageSize, interpolation: InterpolationFlags.Nearest);

    float[] imageData = BgrMatToRgbImage(frameResized, darknet.NetworkDimensions);

    var predictions = darknet.Predict(imageData);

    foreach (Prediction prediction in predictions)
    {
        Console.WriteLine($"Class: {prediction.ClassName}, Confidence: {prediction.Confidence:P2}");
    }
}

static float[] BgrMatToRgbImage(Mat mat, NetworkDimensions dimensions)
{
    // This code assumes the mat object is in OpenCV's default BGR format!

    // TODO: COLOR this function assumes 3-channel images

    // create 3 "views" into 1 large "single-channel" image, one each for B, G, and R
    using var result = new Mat(mat.Rows * 3, mat.Cols, MatType.CV_8UC1);

    var views = new Mat[]
    {
        result.RowRange(mat.Rows * 0, mat.Rows * 1),    // B
        result.RowRange(mat.Rows * 1, mat.Rows * 2),    // G  
        result.RowRange(mat.Rows * 2, mat.Rows * 3),    // R
    };

    Cv2.Split(mat, out views);

    // convert the results to floating point, and divide by 255 to normalize between 0.0 - 1.0
    using var tmp = new Mat(dimensions.Height * 3, dimensions.Width, MatType.CV_32FC1);
    result.ConvertTo(tmp, MatType.CV_32FC1, 1.0 / 255.0);

    // copy the normalized float data to the DarknetImage data array
    var floatData = new float[tmp.Rows * tmp.Cols];
    tmp.GetArray(out floatData);

    // dispose the views
    foreach (var view in views)
    {
        view?.Dispose();
    }

    return floatData;
}
