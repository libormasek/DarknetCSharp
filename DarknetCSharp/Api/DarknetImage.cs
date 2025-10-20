using System.Runtime.InteropServices;

namespace DarknetCSharp.Api;

[StructLayout(LayoutKind.Sequential)]
public struct DarknetImage
{
    public int w;           // width
    public int h;           // height
    public int c;           // channel
    public IntPtr data;     // float* data - normalized floats
}