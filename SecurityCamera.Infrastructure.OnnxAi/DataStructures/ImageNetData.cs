using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using Microsoft.ML.Data;

namespace SecurityCamera.Infrastructure.OnnxAi.DataStructures
{
    public class ImageNetData
    {
        [LoadColumn(0)]
        public required string ImagePath;

        [LoadColumn(1)]
        public required string Label;

        public static IEnumerable<ImageNetData> ReadFromByteArray(string imageLocalFilePath, string imageName)
        {
            return new  ImageNetData[] { new ImageNetData { ImagePath = imageLocalFilePath, Label = imageName } };
        }
    }
}