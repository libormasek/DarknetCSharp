namespace DarknetCSharp;

public class DarknetConfig
{
    public string ConfigurationFilename { get; set; }
    public string NamesFilename { get; set; }

    public string WeightsFilename { get; set; }

    public int? GpuIndex { get; set; }

    public float DetectionThreshold { get; set; } = 0.25f;

    public float HierarchyThreshold { get; set; } = 0.5f;

    public float NonMaximalSuppressionThreshold { get; set; } = 0.45f;

    public DarknetConfig(string configurationFilename, string namesFileName)
    {
        ConfigurationFilename = configurationFilename ?? string.Empty;
        NamesFilename = namesFileName ?? string.Empty;
        WeightsFilename = Path.ChangeExtension(ConfigurationFilename, ".weights") ?? string.Empty;
    }
}