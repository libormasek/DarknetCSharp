using System.Runtime.InteropServices;

namespace DarknetCSharp.Api;

[StructLayout(LayoutKind.Sequential)]
public struct DarknetDetection
{
    public DarknetBox bbox;
    public int classes;
    public int best_class_idx;
    public IntPtr prob;        // float* - points to array of class probabilities
    public IntPtr mask;        // float* 
    public float objectness;
    public int sort_class;
    public IntPtr uc;          // float* - uncertainty values
    public int points;
    public IntPtr embeddings;  // float*
    public int embedding_size;
    public float sim;
    public int track_id;
}