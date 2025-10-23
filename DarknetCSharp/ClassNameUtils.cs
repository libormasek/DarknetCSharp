namespace DarknetCSharp;

public static class ClassNameUtils
{
    /// <summary>
    /// Loads class names from a names file, returning each non-empty line as a separate entry.
    /// </summary>
    /// <remarks>Empty or whitespace-only lines in the file are ignored. Leading and trailing whitespace is
    /// trimmed from each class name.</remarks>
    /// <param name="namesFilename">The path to the file containing class names, with one name per line. Cannot be null or empty.</param>
    /// <returns>An array of class names read from the specified file. Returns an empty array if the file does not exist or the
    /// path is null or empty.</returns>
    public static string[] LoadClassNames(string namesFilename)
    {
        if (string.IsNullOrEmpty(namesFilename) || !File.Exists(namesFilename))
        {
            return [];
        }

        return File.ReadAllLines(namesFilename)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .ToArray();
    }
}