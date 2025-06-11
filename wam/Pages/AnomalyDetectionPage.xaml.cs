using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;

namespace wam.Pages
{
    public partial class AnomalyDetectionPage : UserControl
    {
        public AnomalyDetectionPage()
        {
            InitializeComponent();
            DetectAnomalies();
        }

        public class SystemUsage
        {
            public float Value { get; set; }
        }

        public class SystemUsagePrediction
        {
            [VectorType(3)]
            public double[] PredictionResult { get; set; }
        }

        private void DetectAnomalies()
        {
            var context = new MLContext();

            // Örnek CPU veya RAM verisi
            var samples = new List<SystemUsage>
            {
                new SystemUsage { Value = 30 },
                new SystemUsage { Value = 31 },
                new SystemUsage { Value = 32 },
                new SystemUsage { Value = 95 }, // Anomali gibi
                new SystemUsage { Value = 33 },
                new SystemUsage { Value = 34 },
                new SystemUsage { Value = 96 }, // Anomali gibi
                new SystemUsage { Value = 35 }
            };

            var data = context.Data.LoadFromEnumerable(samples);

            var pipeline = context.Transforms.DetectIidAnomaly(
                outputColumnName: nameof(SystemUsagePrediction.PredictionResult),
                inputColumnName: nameof(SystemUsage.Value),
                confidence: 95,
                pvalueHistoryLength: 5);

            var model = pipeline.Fit(data);
            var transformed = model.Transform(data);

            var predictions = context.Data.CreateEnumerable<SystemUsagePrediction>(transformed, reuseRowObject: false);

            var result = new List<string>();
            int index = 0;

            foreach (var prediction in predictions)
            {
                var status = prediction.PredictionResult[0] == 1 ? "Anomali" : "Normal";
                result.Add($"[{index}] {status} - Değer: {samples[index].Value}");
                index++;
            }

            AnomalyDataGrid.ItemsSource = result;
        }
    }
}
