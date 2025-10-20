using DarknetCSharp.Api;

namespace DarknetCSharp;

public class Darknet : IDisposable
{
    private IntPtr _networkPtr;

    private readonly NetworkDimensions _networkDimensions;

    private string[] _classNames = [];

    public Darknet(DarknetConfig config)
    {
        _networkPtr = DarknetApi.LoadNeuralNetwork(
            config.ConfigurationFilename,
            config.NamesFilename,
            config.WeightsFilename);

        if (config.GpuIndex.HasValue)
        {
            DarknetApi.SetGpuIndex(config.GpuIndex.Value);
        }

        DarknetApi.ShowVersionInfo();

        DarknetApi.NetworkDimensions(_networkPtr, out int w, out int h, out int c);

        _networkDimensions = new NetworkDimensions()
        {
            Width = w,
            Height = h,
            Channels = c
        };
    }

    public ICollection<Prediction> Predict(string fileName)
    {
        var results = new List<Prediction>();

        DarknetImage image = DarknetApi.LoadImageV2(fileName,
            _networkDimensions.Width,
            _networkDimensions.Height,
            _networkDimensions.Channels);

        // TODO - need to call free_image

        DarknetApi.NetworkPredictImage(_networkPtr, image);

        // Get detections
        IntPtr detectionsPtr = DarknetApi.GetNetworkBoxes(
            _networkPtr,
            _networkDimensions.Width, _networkDimensions.Height,     // network dimensions
            0.5f,      // detection threshold
            0.5f,          // hier threshold
            IntPtr.Zero,   // map (usually null)
            1,             // relative coordinates
            out int numDetections,
            0              // letter boxing
        );

        if (detectionsPtr != IntPtr.Zero && numDetections > 0)
        {
            // TODO Apply Non-Maximum Suppression
            //do_nms_sort(detectionsPtr, numDetections, _classNames.Length, 0.1f);

            // Marshal the detection array
            var detections = DarknetDetectionUtils.MarshalDetectionArray(
                    detectionsPtr,
                    numDetections,
                    _classNames);

            results.AddRange(detections);

            // Free the detection memory
            DarknetApi.FreeDetections(detectionsPtr, numDetections);
        }

        // TODO - free_image

        return results;
    }

    //public ICollection<Prediction> Predict(byte[] imageData)
    //{
    //    // TODO implement detection logic here
    //    return Array.Empty<Prediction>();
    //}

    public void Dispose()
    {
        if (_networkPtr != IntPtr.Zero)
        {
            DarknetApi.FreeNeuralNetwork(ref _networkPtr);
        }
    }
}