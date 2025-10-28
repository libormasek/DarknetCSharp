using DarknetCSharp.Api;

namespace DarknetCSharp;

public class Darknet : IDisposable
{
    private IntPtr _networkPtr;

    private readonly NetworkDimensions _networkDimensions;

    private readonly string[] _classNames;

    private readonly DarknetConfig _config;

    public Darknet(DarknetConfig config)
    {
        _config = config;

        if (config.GpuIndex.HasValue)
        {
            DarknetApi.SetGpuIndex(config.GpuIndex.Value);
        }
        
        _networkPtr = DarknetApi.LoadNeuralNetwork(
            _config.ConfigurationFilename,
            _config.NamesFilename,
            _config.WeightsFilename);

        DarknetApi.ShowVersionInfo();

        DarknetApi.NetworkDimensions(_networkPtr, out int w, out int h, out int c);

        _networkDimensions = new NetworkDimensions()
        {
            Width = w,
            Height = h,
            Channels = c
        };

        _classNames = ClassNameUtils.LoadClassNames(_config.NamesFilename);
    }

    public Detection[] Predict(float[] imageData)
    {
        DarknetApi.NetworkPredictPtr(_networkPtr, imageData);

        IntPtr detectionsPtr = IntPtr.Zero;
        int numberOfDetections = 0;

        try
        {
            // Get detections
            detectionsPtr = DarknetApi.GetNetworkBoxes(
                _networkPtr,
                _networkDimensions.Width, _networkDimensions.Height,
                _config.DetectionThreshold,
                _config.HierarchyThreshold,
                IntPtr.Zero,
                1, // relative coordinates
                out numberOfDetections,
                0 // letter boxing
            );

            if (detectionsPtr != IntPtr.Zero && numberOfDetections > 0)
            {
                // Apply Non-Maximum Suppression
                DarknetApi.DoNmsSort(
                    detectionsPtr,
                    numberOfDetections,
                    _classNames.Length,
                    _config.NonMaximalSuppressionThreshold);

                Detection[] detections = DarknetDetectionUtils.ProcessDetections(
                    detectionsPtr,
                    numberOfDetections,
                    _config.DetectionThreshold,
                    _classNames);

                return detections;
            }
        }
        finally
        {
            if (detectionsPtr != IntPtr.Zero && numberOfDetections > 0)
            {
                DarknetApi.FreeDetections(detectionsPtr, numberOfDetections);
            }
        }

        return [];
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