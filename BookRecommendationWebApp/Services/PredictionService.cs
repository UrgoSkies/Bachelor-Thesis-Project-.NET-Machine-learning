using Microsoft.ML;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class PredictionService
{
    private readonly MLContext _mlContext;
    private ITransformer _model;
    private PredictionEngine<BookRatingData, BookRatingPrediction> _predictionEngine;

    public PredictionService(string modelPath)
    {
        _mlContext = new MLContext();
        LoadModel(modelPath);
    }

    private void LoadModel(string modelPath)
    {
        using var stream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        _model = _mlContext.Model.Load(stream, out var modelInputSchema);
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<BookRatingData, BookRatingPrediction>(_model);
    }

    public void UpdateModel(ITransformer model)
    {
        _model = model;
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<BookRatingData, BookRatingPrediction>(_model);
    }

    public float PredictRating(float userId, float isbn)
    {
        var testData = new BookRatingData { UserId = userId, ISBN = isbn };
        var prediction = _predictionEngine.Predict(testData);
        return prediction.Score;
    }

    public List<BookRecommendation> GetRecommendationsForUser(float userId, List<BookRatingData> ratings)
    {
        var recommendations = new List<BookRecommendation>();

        var userRatings = ratings.Where(r => r.UserId == userId).ToList();
        var otherRatings = ratings.Where(r => r.UserId != userId).ToList();

        var commonBooks = userRatings.Select(r => r.ISBN).Intersect(otherRatings.Select(r => r.ISBN));

        foreach (var isbn in commonBooks)
        {
            var score = PredictRating(userId, isbn);
            recommendations.Add(new BookRecommendation
            {
                BookTitle = ratings.First(r => r.ISBN == isbn).BookTitle,
                PredictedRating = score
            });
        }

        return recommendations.OrderByDescending(r => r.PredictedRating).Take(5).ToList();
    }
}
