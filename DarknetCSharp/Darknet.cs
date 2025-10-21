using DarknetCSharp.Api;

namespace DarknetCSharp;

public class Darknet : IDisposable
{
    private IntPtr _networkPtr;

    private readonly NetworkDimensions _networkDimensions;

    private string[] _classNames = [];

    private readonly DarknetConfig _config;

    public Darknet(DarknetConfig config)
    {
        _config = config;

        _networkPtr = DarknetApi.LoadNeuralNetwork(
            _config.ConfigurationFilename,
            _config.NamesFilename,
            _config.WeightsFilename);

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

    public ICollection<Prediction> Predict(float[] imageData)
    {
        var results = new List<Prediction>();

        DarknetApi.NetworkPredictPtr(_networkPtr, imageData);

        // Get detections
        IntPtr detectionsPtr = DarknetApi.GetNetworkBoxes(
            _networkPtr,
            _networkDimensions.Width, _networkDimensions.Height,
            _config.DetectionThreshold,
            _config.HierarchyThreshold,
            IntPtr.Zero,
            1, // relative coordinates
            out int numDetections,
            0 // letter boxing
        );

        if (detectionsPtr != IntPtr.Zero && numDetections > 0)
        {
            // Apply Non-Maximum Suppression
            DarknetApi.DoNmsSort(
                detectionsPtr,
                numDetections,
                _classNames.Length,
                _config.NonMaximalSuppressionThreshold);

            // Marshal the detection array
            var detections = DarknetDetectionUtils.MarshalDetectionArray(
                detectionsPtr,
                numDetections,
                _classNames);

            results.AddRange(detections);

            // Free the detection memory
            DarknetApi.FreeDetections(detectionsPtr, numDetections);
        }

        return results;
    }

    public void Dispose()
    {
        if (_networkPtr != IntPtr.Zero)
        {
            DarknetApi.FreeNeuralNetwork(ref _networkPtr);
        }
    }

    public NetworkDimensions NetworkDimensions => _networkDimensions;
}