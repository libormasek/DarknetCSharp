namespace DarknetCSharp;

public class DarknetConfig
{
    public string ConfigurationFilename { get; set; } = string.Empty;
    public string NamesFilename { get; set; } = string.Empty;

    public string WeightsFilename { get; set; } = string.Empty;

    public DarknetConfig(string configurationFilename)
    {
        ConfigurationFilename = configurationFilename ?? string.Empty;
        NamesFilename = Path.ChangeExtension(ConfigurationFilename, ".names") ?? string.Empty;
        WeightsFilename = Path.ChangeExtension(ConfigurationFilename, ".weights") ?? string.Empty;
    }
}