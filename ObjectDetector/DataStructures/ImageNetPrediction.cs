using Microsoft.ML.Data;

namespace ObjectDetector.DataStructures
{
    public class ImageNetPrediction
    {
        [ColumnName("grid")]
        public float[] PredictedLabels;
    }
}
