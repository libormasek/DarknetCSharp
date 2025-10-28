using DarknetCSharp;
using System.Collections.Concurrent;
using System.Diagnostics;

var config = new DarknetConfig(
    @"cfg\yolov4-tiny.cfg",
    @"cfg\coco.names")
{
    DetectionThreshold = 0.1f,
    GpuIndex = 0
};

using var darknet = new Darknet(config);

string rtspUrl = "rtsp://";

var networkDimensions = darknet.NetworkDimensions;

// Pre-allocated buffer for image data to input into Darknet
var chwBuffer = new float[networkDimensions.Width * networkDimensions.Height * networkDimensions.Channels];

// Frame buffer for processing
var frameQueue = new ConcurrentQueue<byte[]>();
var cancellationTokenSource = new CancellationTokenSource();

Console.WriteLine("Starting RTSP stream processing with FFMpeg...");
Console.WriteLine("Press 'q' to quit");

// Start FFMpeg capture in background task
var captureTask = Task.Run(() => CaptureFrames(rtspUrl, frameQueue, networkDimensions, cancellationTokenSource.Token));

// Process frames
await ProcessFrames(darknet, networkDimensions, chwBuffer, frameQueue, cancellationTokenSource);

cancellationTokenSource.Cancel();
await captureTask;

static async Task CaptureFrames(string rtspUrl, ConcurrentQueue<byte[]> frameQueue, NetworkDimensions networkDimensions, CancellationToken cancellationToken)
{
    try
    {
        var frameSize = networkDimensions.Width * networkDimensions.Height * 3; // RGB24

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Use FFMpeg to capture frames and save them temporarily
                var ffmpegProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-i \"{rtspUrl}\" " +
                                    $"-f rawvideo " +
                                    $"-pix_fmt rgb24 -s {networkDimensions.Width}x{networkDimensions.Height} " +
                                    $"-r 10 -",
                        // TODO - use NVidia decoding with CUDA
                        //Arguments = $"-hwaccel cuda -hwaccel_output_format cuda " +
                        //            $"-i \"{rtspUrl}\" " +
                        //            $"-vf \"scale_cuda={networkDimensions.Width}:{networkDimensions.Height},hwdownload,format=rgb24\" " +
                        //            $"-f rawvideo -pix_fmt rgb24 " +
                        //            $"-r 10 -",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                ffmpegProcess.Start();

                // Monitor stderr for FFMpeg logging
                var stderrTask = Task.Run(() => MonitorFFmpegErrors(ffmpegProcess, cancellationToken));

                var buffer = new byte[frameSize];

                await using var stdout = ffmpegProcess.StandardOutput.BaseStream;
                
                while (!cancellationToken.IsCancellationRequested && !ffmpegProcess.HasExited)
                {
                    int totalBytesRead = 0;
                    while (totalBytesRead < frameSize && !cancellationToken.IsCancellationRequested)
                    {
                        int bytesRead = await stdout.ReadAsync(buffer,
                            totalBytesRead,
                            frameSize - totalBytesRead,
                            cancellationToken);

                        if (bytesRead == 0) break;

                        totalBytesRead += bytesRead;
                    }

                    if (totalBytesRead == frameSize)
                    {
                        frameQueue.Enqueue(buffer);

                        // Limit queue size to prevent memory issues
                        while (frameQueue.Count > 5)
                        {
                            frameQueue.TryDequeue(out _);
                        }
                    }
                }

                if (!ffmpegProcess.HasExited)
                {
                    ffmpegProcess.Kill();
                }
                
                ffmpegProcess.WaitForExit();

                // Wait for stderr monitoring to complete
                await stderrTask;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                Console.WriteLine($"FFMpeg process error: {ex.Message}");
                await Task.Delay(1000, cancellationToken); // Wait before retry
            }
        }
    }
    catch (OperationCanceledException)
    {
        // Expected when cancellation is requested
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in frame capture: {ex.Message}");
    }
}

static async Task ProcessFrames(Darknet darknet, NetworkDimensions networkDimensions, float[] chwBuffer,
    ConcurrentQueue<byte[]> frameQueue, CancellationTokenSource cancellationTokenSource)
{
    while (!cancellationTokenSource.Token.IsCancellationRequested)
    {
        if (frameQueue.TryDequeue(out var frame))
        {
            // Convert to normalized RGB CHW float[] for Darknet
            RgbImageToChwBuffer(frame, networkDimensions, chwBuffer);
            
            Detection[] detections = darknet.Predict(chwBuffer);

            if (detections.Length > 0)
            {
                Console.WriteLine($"Processed frame with {detections.Length} detections");
            }

            foreach (var detection in detections)
            {
                Console.WriteLine($"  {detection.ClassName}: {detection.Probability:P1}");
            }
        }
        else
        {
            await Task.Delay(10, cancellationTokenSource.Token);
        }
        
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Escape)
            {
                cancellationTokenSource.Cancel();
                break;
            }
        }
    }
}

static unsafe void RgbImageToChwBuffer(byte[] rgbBytes, NetworkDimensions dimensions, float[] buffer)
{
    int w = dimensions.Width, h = dimensions.Height, hw = w * h;
    float inv255 = 1f / 255f;

    // Pin the arrays and get pointers for direct memory access
    fixed (byte* rgbPtr = rgbBytes)
    fixed (float* bufferPtr = buffer)
    {
        byte* srcPtr = rgbPtr;
        float* rPtr = bufferPtr;                    // R channel starts at offset 0
        float* gPtr = bufferPtr + hw;               // G channel starts at offset hw
        float* bPtr = bufferPtr + (2 * hw);         // B channel starts at offset 2*hw

        // Process all pixels sequentially
        for (int i = 0; i < hw; i++)
        {
            // Read RGB values from source (3 bytes per pixel)
            byte r = *srcPtr++;
            byte g = *srcPtr++;
            byte b = *srcPtr++;

            // Write normalized values to CHW format buffers
            *rPtr++ = r * inv255;
            *gPtr++ = g * inv255;
            *bPtr++ = b * inv255;
        }
    }
}

static async Task MonitorFFmpegErrors(Process ffmpegProcess, CancellationToken cancellationToken)
{
    try
    {
        using var stderr = ffmpegProcess.StandardError;
        string line;

        while (!cancellationToken.IsCancellationRequested && !ffmpegProcess.HasExited)
        {
            line = await stderr.ReadLineAsync();
            if (line == null) break;

            // Filter and display important FFmpeg messages
            if (line.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("could not", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("unable", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("stream", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("codec", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Input #", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Output #", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("fps=", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[FFmpeg] {line}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error monitoring FFmpeg stderr: {ex.Message}");
    }
}
