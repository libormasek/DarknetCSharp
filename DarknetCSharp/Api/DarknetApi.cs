using System.Runtime.InteropServices;

namespace DarknetCSharp.Api;

public static class DarknetApi
{
    private const string YoloLibraryName = "darknet.dll";

    [DllImport(YoloLibraryName, EntryPoint = "darknet_load_neural_network")]
    public static extern IntPtr LoadNeuralNetwork(string configurationFilename, string namesFilename, string weightsFilename);

    [DllImport(YoloLibraryName, EntryPoint = "darknet_set_gpu_index")]
    public static extern void SetGpuIndex(int gpuIndex);

    [DllImport(YoloLibraryName, EntryPoint = "darknet_show_version_info")]
    public static extern void ShowVersionInfo();

    [DllImport(YoloLibraryName, EntryPoint = "darknet_network_dimensions")]
    public static extern void NetworkDimensions(IntPtr networkPtr, out int w, out int h, out int c);

    [DllImport(YoloLibraryName, EntryPoint = "network_predict_ptr")]
    public static extern IntPtr NetworkPredictPtr(IntPtr networkPtr, float[] input);

    [DllImport(YoloLibraryName, EntryPoint = "network_predict_image")]
    public static extern IntPtr NetworkPredictImage(IntPtr ptr, DarknetImage image);

    [DllImport(YoloLibraryName, EntryPoint = "load_image_v2")]
    public static extern DarknetImage LoadImageV2(
        [MarshalAs(UnmanagedType.LPStr)] string filename,
        int desiredWidth,
        int desiredHeight,
        int channels);
    
    [DllImport(YoloLibraryName, EntryPoint = "copy_image_from_bytes")]
    public static extern void CopyImageFromBytes(DarknetImage image, byte[] data);

    [DllImport(YoloLibraryName, EntryPoint = "darknet_free_neural_network")]
    public static extern void FreeNeuralNetwork(ref IntPtr networkPtr);

    [DllImport(YoloLibraryName, EntryPoint = "do_nms_sort")]
    public static extern void DoNmsSort(IntPtr detections, int total, int classes, float threshold);

    [DllImport(YoloLibraryName, EntryPoint = "make_image")]
    public static extern DarknetImage MakeImage(int w, int h, int c);

    [DllImport(YoloLibraryName, EntryPoint = "free_image")]
    public static extern void FreeImage(DarknetImage image);

    [DllImport(YoloLibraryName, EntryPoint = "free_detections")]
    public static extern void FreeDetections(IntPtr detections, int count);

    [DllImport(YoloLibraryName, EntryPoint = "get_network_boxes")]
    public static extern IntPtr GetNetworkBoxes(
        IntPtr networkPtr,
        int w, int h,
        float thresh,
        float hier,
        IntPtr map,
        int relative,
        out int num,
        int letter
    );
}