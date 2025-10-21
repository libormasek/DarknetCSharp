namespace DarknetCSharp;

public class DarknetConfig
{
    public string ConfigurationFilename { get; set; } = string.Empty;
    public string NamesFilename { get; set; } = string.Empty;

    public string WeightsFilename { get; set; } = string.Empty;

    public int? GpuIndex { get; set; }

    public float DetectionThreshold { get; set; } = 0.25f;

    public float HierarchyThreshold { get; set; } = 0.5f;

    public float NonMaximalSuppressionThreshold { get; set; } = 0.45f;

    public DarknetConfig(string configurationFilename)
    {
        ConfigurationFilename = configurationFilename ?? string.Empty;
        //NamesFilename = Path.ChangeExtension(ConfigurationFilename, ".names") ?? string.Empty;
        WeightsFilename = Path.ChangeExtension(ConfigurationFilename, ".weights") ?? string.Empty;
    }
}