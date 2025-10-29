using DarknetCSharp;
using DarknetCSharpExample;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

var config = new DarknetConfig(
    @"D:\Program Files\Darknet\cfg\yolov4-tiny.cfg",
    @"D:\Program Files\Darknet\cfg\coco.names")
{
    DetectionThreshold = 0.2f,
    GpuIndex = 0
};

using var darknet = new Darknet(config);

// Load JPG image
string inputImagePath = @"F:\test.JPG";
string outputImagePath = @"F:\output.JPG";

using var image = Image.Load<Rgb24>(inputImagePath);

var networkDimensions = darknet.NetworkDimensions;
var imageSize = new Size(networkDimensions.Width, networkDimensions.Height);

// Create a resized copy for inference
using var resizedImage = image.Clone(ctx => ctx.Resize(imageSize));

// Convert to normalized RGB CHW float[] for Darknet
var chwBuffer = new float[networkDimensions.Width * networkDimensions.Height * networkDimensions.Channels];
ImageUtils.RgbToChw(resizedImage, networkDimensions, chwBuffer);

// Run detection
Detection[] detections = darknet.Predict(chwBuffer);

// Draw detections on the original image
ImageUtils.DrawDetectionsOnImage(image, detections);

// Save result
image.Save(outputImagePath);

Console.WriteLine($"Processed {inputImagePath} with {detections.Length} detections");
Console.WriteLine($"Output saved to {outputImagePath}");


