using System.Runtime.InteropServices;

namespace DarknetCSharp.Api;

[StructLayout(LayoutKind.Sequential)]
public struct DarknetBox
{
    public float x;
    public float y;
    public float w;
    public float h;
}