using DarknetCSharp;

var config = new DarknetConfig(@"D:\Program Files\Darknet\cfg\yolov4-tiny.cfg");

using var darknet = new Darknet(config);

ICollection<Prediction> predictions = darknet.Predict(@"F:\New folder\imgSceneA.jpg");

foreach (Prediction prediction in predictions)
{
    Console.WriteLine($"Class: {prediction.ClassName}, Confidence: {prediction.Confidence:P2}");
}
