using System.Reflection;
using System.Runtime.InteropServices;

namespace DarknetCSharp.Api;

public static class DarknetApi
{
    static DarknetApi()
    {
        NativeLibrary.SetDllImportResolver(typeof(DarknetApi).Assembly, ImportResolver);
    }

    private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName == DarknetLibraryName)
        {
            string libPath = 
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{DarknetLibraryName}.dll" :
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? $"lib{DarknetLibraryName}.so" :
                    throw new PlatformNotSupportedException();

            return NativeLibrary.Load(libPath, assembly, searchPath);
        }
        return IntPtr.Zero;
    }

    private const string DarknetLibraryName = "darknet";

    [DllImport(DarknetLibraryName, EntryPoint = "darknet_load_neural_network")]
    public static extern IntPtr LoadNeuralNetwork(string configurationFilename, string namesFilename, string weightsFilename);

    [DllImport(DarknetLibraryName, EntryPoint = "darknet_set_gpu_index")]
    public static extern void SetGpuIndex(int gpuIndex);

    [DllImport(DarknetLibraryName, EntryPoint = "darknet_show_version_info")]
    public static extern void ShowVersionInfo();

    [DllImport(DarknetLibraryName, EntryPoint = "darknet_network_dimensions")]
    public static extern void NetworkDimensions(IntPtr networkPtr, out int w, out int h, out int c);

    [DllImport(DarknetLibraryName, EntryPoint = "network_predict_ptr")]
    public static extern IntPtr NetworkPredictPtr(IntPtr networkPtr, float[] input);

    [DllImport(DarknetLibraryName, EntryPoint = "darknet_free_neural_network")]
    public static extern void FreeNeuralNetwork(ref IntPtr networkPtr);

    [DllImport(DarknetLibraryName, EntryPoint = "do_nms_sort")]
    public static extern void DoNmsSort(IntPtr detections, int total, int classes, float threshold);

    [DllImport(DarknetLibraryName, EntryPoint = "free_detections")]
    public static extern void FreeDetections(IntPtr detections, int count);

    [DllImport(DarknetLibraryName, EntryPoint = "get_network_boxes")]
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