using Microsoft.AspNetCore.Mvc;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

namespace BookRecommendationWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModelController : ControllerBase
    {
        private const string ModelPath = "model.zip";
        private readonly PredictionService _predictionService;

        public ModelController(PredictionService predictionService)
        {
            _predictionService = predictionService;
        }

        [HttpPost("train")]
        public IActionResult TrainAndTestModel([FromBody] string dataPath)
        {
            var mlContext = new MLContext(seed: 0);
            ITransformer model = null;
            List<BookRatingData> records = null;

            try
            {
                records = ReadDataFromCsv(dataPath);
                var dataView = mlContext.Data.LoadFromEnumerable(records);
                PrintFirstFiveRecords(mlContext, dataView);
                CheckRatingRange(mlContext, dataView);

                // Train model 
                model = BuildAndTrainModel(mlContext, dataView);

                // Evaluate model 
                EvaluateModel(mlContext, model, dataView);

                // Save model
                SaveModel(mlContext, model, ModelPath);

                // Update prediction service with new model
                _predictionService.UpdateModel(model);
            }
            catch (Exception ex)
            {
                return BadRequest($"An error occurred: {ex.Message}");
            }

            return Ok("Model training and evaluation complete. Model saved and updated.");
        }

        private static List<BookRatingData> ReadDataFromCsv(string filePath)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";"
            };

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecords<BookRatingData>().ToList();
                Console.WriteLine($"Loaded {records.Count} records from CSV file.");
                return records;
            }
        }

        private static void PrintFirstFiveRecords(MLContext mlContext, IDataView dataView)
        {
            var records = mlContext.Data.CreateEnumerable<BookRatingData>(dataView, reuseRowObject: false).Take(5);
            Console.WriteLine("First 5 records from the CSV file:");
            foreach (var record in records)
            {
                Console.WriteLine($"Country: {record.Country}, ISBN: {record.ISBN}, Title: {record.BookTitle}, Author: {record.BookAuthor}, Publisher: {record.Publisher}, UserID: {record.UserId}, Age: {record.Age}, Year: {record.YearOfPublication}, Rating: {record.BookRating}");
            }
        }

        private static void CheckRatingRange(MLContext mlContext, IDataView dataView)
        {
            var ratings = mlContext.Data.CreateEnumerable<BookRatingData>(dataView, reuseRowObject: false).Select(r => r.BookRating).ToList();
            float minRating = ratings.Min();
            float maxRating = ratings.Max();
            Console.WriteLine($"Ratings range in data: Min = {minRating}, Max = {maxRating}");
        }

        private static ITransformer BuildAndTrainModel(MLContext mlContext, IDataView trainSet)
        {
            var pipeline = mlContext.Transforms.Conversion.MapValueToKey("UserId", "UserId")
                            .Append(mlContext.Transforms.Conversion.MapValueToKey("ISBN", "ISBN"))
                            .Append(mlContext.Recommendation().Trainers.MatrixFactorization(new MatrixFactorizationTrainer.Options()
                            {
                                LabelColumnName = "BookRating",
                                MatrixColumnIndexColumnName = "UserId",
                                MatrixRowIndexColumnName = "ISBN",
                                ApproximationRank = 10,
                                NumberOfIterations = 32767,
                                Quiet = true
                            }))
                            .Append(mlContext.Transforms.Conversion.MapKeyToValue("UserId", "UserId"))
                            .Append(mlContext.Transforms.Conversion.MapKeyToValue("ISBN", "ISBN"));

            Console.WriteLine("Training model...");
            var model = pipeline.Fit(trainSet);
            Console.WriteLine("Model training complete.");
            return model;
        }

        private static void EvaluateModel(MLContext mlContext, ITransformer model, IDataView dataView)
        {
            Console.WriteLine("Evaluating model...");
            var predictions = model.Transform(dataView);
            var metrics = mlContext.Regression.Evaluate(predictions, labelColumnName: "BookRating", scoreColumnName: "Score");

            Console.WriteLine("Model evaluation complete.");
            Console.WriteLine($"Mean Absolute Error: {metrics.MeanAbsoluteError}");
            Console.WriteLine($"Root Mean Squared Error: {metrics.RootMeanSquaredError}");
            Console.WriteLine($"RSquared: {metrics.RSquared}");
        }

        public static void SaveModel(MLContext mlContext, ITransformer model, string modelPath)
        {
            mlContext.Model.Save(model, null, modelPath);
            Console.WriteLine($"Model saved to {modelPath}");
        }
    }

    public class BookRatingData
    {
        public string Country { get; set; }
        public long ISBN { get; set; }
        [Name("Book-Title")]
        public string BookTitle { get; set; }
        [Name("Book-Author")]
        public string BookAuthor { get; set; }
        public string Publisher { get; set; }
        [Name("User-ID")]
        public float UserId { get; set; }
        public float Age { get; set; }
        [Name("Year-Of-Publication")]
        public float YearOfPublication { get; set; }
        [Name("Book-Rating")]
        public float BookRating { get; set; }
    }

    public class BookRatingPrediction
    {
        public float Label;
        public float Score;
    }
}
