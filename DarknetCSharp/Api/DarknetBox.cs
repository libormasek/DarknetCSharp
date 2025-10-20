using System.Runtime.InteropServices;

namespace DarknetCSharp.Api;

[StructLayout(LayoutKind.Sequential)]
public struct DarknetBox
{
    public float x; // center X (normalized 0-1)
    public float y; // center Y (normalized 0-1) 
    public float w; // width (normalized 0-1)
    public float h; // height (normalized 0-1)
}