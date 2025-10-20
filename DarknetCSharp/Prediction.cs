using DarknetCSharp.Api;

namespace DarknetCSharp;

public class Prediction
{
    public string ClassName { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public DarknetBox BoundingBox { get; set; }
}