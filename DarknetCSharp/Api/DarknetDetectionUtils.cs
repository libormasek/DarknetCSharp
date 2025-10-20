using System.Runtime.InteropServices;

namespace DarknetCSharp.Api;

public static class DarknetDetectionUtils
{
    public static List<Prediction> MarshalDetectionArray(IntPtr detectionsPtr, int count, string[] classNames)
    {
        var results = new List<Prediction>();
        int detectionSize = Marshal.SizeOf<DarknetDetection>();

        for (int i = 0; i < count; i++)
        {
            // Calculate pointer to this detection
            IntPtr detectionPtr = IntPtr.Add(detectionsPtr, i * detectionSize);

            // Marshal the detection structure
            var detection = Marshal.PtrToStructure<DarknetDetection>(detectionPtr);

            // Find the best class and confidence
            if (detection.prob != IntPtr.Zero && detection.classes > 0)
            {
                // Marshal the probability array
                float[] probabilities = new float[detection.classes];
                Marshal.Copy(detection.prob, probabilities, 0, detection.classes);

                // Find the class with highest probability
                float maxProb = 0f;
                int bestClass = -1;

                for (int j = 0; j < probabilities.Length; j++)
                {
                    if (probabilities[j] > maxProb)
                    {
                        maxProb = probabilities[j];
                        bestClass = j;
                    }
                }

                // Only include detections above threshold
                if (maxProb > 0.1f )//&& bestClass >= 0 && bestClass < classNames.Length)
                {
                    results.Add(new Prediction()
                    {
                        ClassName = bestClass.ToString(),//classNames[bestClass],
                        Confidence = maxProb,
                        BoundingBox = detection.bbox
                    });
                }
            }
        }

        return results;
    }
}