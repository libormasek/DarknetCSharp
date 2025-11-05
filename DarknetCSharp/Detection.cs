using DarknetCSharp.Api;

namespace DarknetCSharp;

public class Detection
{
    public DateTime TimeStampUtc { get; set; }
    public int ClassIndex { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public float Probability { get; set; }
    public DarknetBox BoundingBox { get; set; }
}