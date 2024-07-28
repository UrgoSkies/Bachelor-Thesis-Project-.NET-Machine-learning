using System;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

public class Program
{
    private const string ModelPath = @"C:\Users\Rinald\Desktop\Data-cleared\model.zip";
    public static void Main(string[] args)
    {
        string dataPath = "C:\\Users\\Rinald\\Desktop\\Data-cleared\\ready.csv";
        var mlContext = new MLContext(seed: 0);
        ITransformer model = null;
        try
        {
            // Load data from CSV file
            var dataView = ReadDataFromCsv(mlContext, dataPath);
            // Print the first five records from the dataset
            PrintFirstFiveRecords(mlContext, dataView);

            // Train the model
            model = BuildAndTrainModel(mlContext, dataView);

            // Evaluate the model
            EvaluateModel(mlContext, model, dataView);

            // Save the model
            SaveModel(mlContext, model, ModelPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

        if (model != null)
        {
            float userId = 53;
            float isbn = 0679865691;
            // Predict the rating for a single book
            PredictSingleBookRating(mlContext, model, userId, isbn);
            // Predict the top books for a user
            PredictTopBooksForUser(mlContext, model, userId, dataPath);
        }
    }

    // Read data from a CSV file and return an IDataView
    private static IDataView ReadDataFromCsv(MLContext mlContext, string filePath)
    {
        var dataView = mlContext.Data.LoadFromTextFile<BookRatingData>(filePath, hasHeader: true, separatorChar: ';');
        return dataView;
    }

    // Print the first five records from the dataset
    private static void PrintFirstFiveRecords(MLContext mlContext, IDataView dataView)
    {
        var records = mlContext.Data.CreateEnumerable<BookRatingData>(dataView, reuseRowObject: false).Take(5);
        Console.WriteLine("First 5 records from the CSV file:");
        foreach (var record in records)
        {
            Console.WriteLine($"Country: {record.Country}, ISBN: {record.ISBN}, Title: {record.BookTitle}, Author: {record.BookAuthor}, Publisher: {record.Publisher}, UserID: {record.UserId}, Age: {record.Age}, Year: {record.YearOfPublication}, Rating: {record.BookRating}");
        }
    }

    // Build and train the model
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
                            LearningRate = 0.0112734883460408,
                            NumberOfIterations = 32767,
                            Quiet = true
                        }))
                        .Append(mlContext.Transforms.Conversion.MapKeyToValue("UserId", "UserId"))
                        .Append(mlContext.Transforms.Conversion.MapKeyToValue("ISBN", "ISBN"));

        return pipeline.Fit(trainSet);
    }

    // Evaluate the model using the test data
    private static void EvaluateModel(MLContext mlContext, ITransformer model, IDataView dataView)
    {
        var predictions = model.Transform(dataView);
        var metrics = mlContext.Regression.Evaluate(predictions, labelColumnName: "BookRating", scoreColumnName: "Score");

        Console.WriteLine("Model evaluation complete.");
        Console.WriteLine($"Mean Absolute Error: {metrics.MeanAbsoluteError}");
        Console.WriteLine($"Root Mean Squared Error: {metrics.RootMeanSquaredError}");
        Console.WriteLine($"RSquared: {metrics.RSquared}");
    }

    // Predict the rating for a single book for a specific user
    private static void PredictSingleBookRating(MLContext mlContext, ITransformer model, float userId, float isbn)
    {
        var predictionEngine = mlContext.Model.CreatePredictionEngine<BookRatingData, BookRatingPrediction>(model);
        var testData = new BookRatingData
        {
            UserId = userId,
            ISBN = isbn
        };

        var prediction = predictionEngine.Predict(testData);
        Console.WriteLine($"Predicted rating for user {userId} on book {isbn} is {prediction.Score}");
    }

    // Load all unique ISBNs from the dataset
    private static List<float> LoadAllBookISBNs(MLContext mlContext, string filePath)
    {
        var dataView = mlContext.Data.LoadFromTextFile<BookRatingData>(filePath, hasHeader: true, separatorChar: ';');
        var books = mlContext.Data.CreateEnumerable<BookRatingData>(dataView, reuseRowObject: false).ToList();

        var isbnList = books.Select(b => b.ISBN).Distinct().ToList();
        return isbnList;
    }

    // Predict the top books for a specific user
    private static void PredictTopBooksForUser(MLContext mlContext, ITransformer model, float userId, string filePath)
    {
        var allISBNs = LoadAllBookISBNs(mlContext, filePath);
        var predictionEngine = mlContext.Model.CreatePredictionEngine<BookRatingData, BookRatingPrediction>(model);
        var predictions = new List<Tuple<float, float>>();

        foreach (var isbn in allISBNs)
        {
            var testData = new BookRatingData { UserId = userId, ISBN = isbn };
            var prediction = predictionEngine.Predict(testData);
            predictions.Add(new Tuple<float, float>(isbn, prediction.Score));
        }

        // Get the top 5 book predictions for the user
        var topPredictions = predictions.OrderByDescending(p => p.Item2).Take(5).ToList();

        Console.WriteLine("Top 5 ratings for user:");
        foreach (var pred in topPredictions)
        {
            Console.WriteLine($"ISBN: {pred.Item1}, rating: {pred.Item2:F2}");
        }
    }

    // Save the trained model to a file
    public static void SaveModel(MLContext mlContext, ITransformer model, string modelPath)
    {
        mlContext.Model.Save(model, null, modelPath);
        Console.WriteLine($"Model saved to {modelPath}");
    }
}

public class BookRatingPrediction
{
    public float Label;
    public float Score;
}

public class BookRatingData
{
    [LoadColumn(0)]
    public string Country;
    [LoadColumn(1)]
    public float ISBN;
    [LoadColumn(2)]
    public string BookTitle;
    [LoadColumn(3)]
    public string BookAuthor;
    [LoadColumn(4)]
    public string Publisher;
    [LoadColumn(5)]
    public float UserId;
    [LoadColumn(6)]
    public float Age;
    [LoadColumn(7)]
    public float YearOfPublication;
    [LoadColumn(8)]
    public float BookRating;
}
