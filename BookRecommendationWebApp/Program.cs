using System;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace ModelTrainer
{
    class Program
    {
        static void Main(string[] args)
        {
            var mlContext = new MLContext();

            // Вывод текущей рабочей директории
            Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");

            // Абсолютный путь к файлу данных
            var trainingDataPath = @"C:\Users\Rinald\source\repos\mlv1\BookRecommendationWebApp\data\book_ratings.csv";

            if (!File.Exists(trainingDataPath))
            {
                Console.WriteLine($"File not found: {trainingDataPath}");
                throw new FileNotFoundException($"File not found: {trainingDataPath}");
            }

            // Загрузите данные
            var dataView = mlContext.Data.LoadFromTextFile<BookRatingData>(trainingDataPath, hasHeader: true, separatorChar: ',');

            // Построение модели
            var dataProcessingPipeline = mlContext.Transforms.Conversion.MapValueToKey(nameof(BookRatingData.UserId))
                .Append(mlContext.Transforms.Conversion.MapValueToKey(nameof(BookRatingData.ISBN)));

            var options = new MatrixFactorizationTrainer.Options
            {
                MatrixColumnIndexColumnName = nameof(BookRatingData.UserId),
                MatrixRowIndexColumnName = nameof(BookRatingData.ISBN),
                LabelColumnName = nameof(BookRatingData.Label),
                NumberOfIterations = 20,
                ApproximationRank = 100
            };

            var trainingPipeline = dataProcessingPipeline.Append(mlContext.Recommendation().Trainers.MatrixFactorization(options));

            // Обучение модели
            var model = trainingPipeline.Fit(dataView);

            // Сохранение модели
            var modelPath = "model.zip";
            mlContext.Model.Save(model, dataView.Schema, modelPath);

            Console.WriteLine("Model training completed and saved to model.zip");

            // Ожидание ввода пользователя для предотвращения немедленного закрытия
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    public class BookRatingData
    {
        [LoadColumn(0)]
        public float UserId { get; set; }

        [LoadColumn(1)]
        public float ISBN { get; set; }

        [LoadColumn(2)]
        public float Label { get; set; } // Рейтинг книги

        [LoadColumn(3)]
        public string BookTitle { get; set; } // Название книги
    }

    public class BookRatingPrediction
    {
        public float Label { get; set; }
        public float Score { get; set; }
    }
}
