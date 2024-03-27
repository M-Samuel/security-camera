using Microsoft.ML.Data;

namespace SecurityCamera.Infrastructure.OnnxAi.DataStructures
{
    public class ImageNetPrediction
    {
        [ColumnName("grid")]
        public required float[] PredictedLabels;
    }
}
