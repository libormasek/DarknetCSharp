using System.Runtime.InteropServices;

namespace DarknetCSharp.Api;

public static class DarknetDetectionUtils
{
    private static readonly int DetectionSize = Marshal.SizeOf<DarknetDetection>();

    /// <summary>
    /// Processes raw detection results from unmanaged memory and returns an array of valid detections that meet the
    /// specified probability threshold.
    /// </summary>
    /// <remarks>This method filters detections based on the specified probability threshold and maps class
    /// indices to their corresponding names. The caller is responsible for ensuring that detectionsPtr points to a
    /// valid memory block and that classNames contains entries for all possible classes.</remarks>
    /// <param name="detectionsPtr">A pointer to the unmanaged memory block containing detection results. Each detection is expected to be
    /// structured according to the native detection format.</param>
    /// <param name="numberOfDetections">The total number of detections to process from the memory block pointed to by detectionsPtr.</param>
    /// <param name="detectionThreshold">The minimum probability required for a detection to be considered valid. Detections with a maximum class
    /// probability below this value are excluded.</param>
    /// <param name="classNames">An array of class names corresponding to the possible detection classes. The index in this array should match
    /// the class index in the detection results.</param>
    /// <returns>An array of Detection objects representing valid detections with a probability greater than or equal to
    /// detectionThreshold. The array will be empty if no detections meet the threshold.</returns>
    public static Detection[] ProcessDetections(
        IntPtr detectionsPtr,
        int numberOfDetections,
        float detectionThreshold,
        string[] classNames)
    {
        var predictions = new Detection[numberOfDetections];

        int validPredictionCount = 0;

        for (int i = 0; i < numberOfDetections; i++)
        {
            IntPtr detectionPtr = IntPtr.Add(detectionsPtr, i * DetectionSize);
            DarknetDetection detection = Marshal.PtrToStructure<DarknetDetection>(detectionPtr);

            if (detection.prob == IntPtr.Zero || detection.classes <= 0)
                continue;

            float[] probabilities = new float[detection.classes];
            Marshal.Copy(detection.prob, probabilities, 0, detection.classes);

            float maxProbability = probabilities[0];
            int bestClassIndex = 0;
            for (int j = 1; j < probabilities.Length; j++)
            {
                if (probabilities[j] > maxProbability)
                {
                    maxProbability = probabilities[j];
                    bestClassIndex = j;
                }
            }

            if (maxProbability >= detectionThreshold)
            {
                predictions[validPredictionCount] = new Detection
                {
                    ClassName = classNames[bestClassIndex],
                    ClassIndex = bestClassIndex,
                    Probability = maxProbability,
                    BoundingBox = detection.bbox
                };

                validPredictionCount++;
            }
        }

        Array.Resize(ref predictions, validPredictionCount);
        return predictions;
    }

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