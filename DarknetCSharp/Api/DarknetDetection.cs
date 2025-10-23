using System.Runtime.InteropServices;

namespace DarknetCSharp.Api;

[StructLayout(LayoutKind.Sequential)]
public struct DarknetDetection
{
    /// <summary>
    /// Bounding box coordinates
    /// </summary>
    public DarknetBox bbox;

    /// <summary>
    /// Number of classes
    /// </summary>
    public int classes;

    /// <summary>
    /// Index of the best class
    /// </summary>
    public int best_class_idx;

    /// <summary>
    /// Points to an array of class probabilities (float*)
    /// </summary>
    public IntPtr prob;

    /// <summary>
    /// Points to a mask for this detection (float*)
    /// </summary>
    public IntPtr mask;

    /// <summary>
    /// Objectness score
    /// </summary>
    public float objectness;

    /// <summary>
    /// Sorted class index
    /// </summary>
    public int sort_class;

    /// <summary>
    /// Pointer to uncertainty values (float*)
    /// </summary>
    public IntPtr uc;

    /// <summary>
    /// Number of points
    /// </summary>
    public int points;

    /// <summary>
    /// Embeddings (float*)
    /// </summary>
    public IntPtr embeddings;

    /// <summary>
    /// Size of embeddings
    /// </summary>
    public int embedding_size;

    /// <summary>
    /// Similarity score
    /// </summary>
    public float sim;

    /// <summary>
    /// Track ID
    /// </summary>
    public int track_id;
}