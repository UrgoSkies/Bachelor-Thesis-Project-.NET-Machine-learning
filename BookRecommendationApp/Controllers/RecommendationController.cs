using Microsoft.AspNetCore.Mvc;
using Microsoft.ML;
using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BookRecommendationApp.Models;

namespace BookRecommendationApp.Controllers
{
    public class RecommendationController : Controller
    {
        private readonly MLContext _mlContext;
        private readonly ITransformer _model;
        private readonly List<Book> _books;

        public RecommendationController()
        {
            _mlContext = new MLContext();
            _model = LoadModel("C:\\Users\\Rinald\\Desktop\\Data-cleared\\model.zip");
            _books = LoadBooks("C:\\Users\\Rinald\\Desktop\\Data-cleared\\ready.csv");
        }

        public IActionResult Index()
        {
            var users = new List<string> { "Артур", "Дима", "Вероника" };
            return View(users);
        }

        [HttpPost]
        public IActionResult Recommend(string user)
        {
            var userId = GetUserId(user);
            var booksToRate = _books.Take(5).ToList();

            // Загружаем существующие оценки
            var existingRatings = RatingStore.Ratings.Where(r => r.UserId == userId).ToList();

            // Заполняем оценки, если они уже существуют
            foreach (var book in booksToRate)
            {
                var existingRating = existingRatings.FirstOrDefault(r => r.ISBN == book.ISBN);
                if (existingRating != null)
                {
                    book.Liked = existingRating.BookRating == 1;
                }
            }

            var model = new UserBooksViewModel
            {
                UserId = userId,
                Books = booksToRate
            };
            return View("RateBooks", model);
        }

        [HttpPost]
        public IActionResult SubmitRatings(UserBooksViewModel model)
        {
            foreach (var book in model.Books)
            {
                var existingRating = RatingStore.Ratings
                    .FirstOrDefault(r => r.UserId == model.UserId && r.ISBN == book.ISBN);

                if (existingRating != null)
                {
                    // Обновляем существующую оценку
                    existingRating.BookRating = book.Liked ? 1 : 0;
                }
                else
                {
                    // Добавляем новую оценку
                    RatingStore.Ratings.Add(new BookRatingData
                    {
                        UserId = model.UserId,
                        ISBN = book.ISBN,
                        BookRating = book.Liked ? 1 : 0
                    });
                }
            }

            var recommendations = GetRecommendations(model.UserId);
            return View("Recommendations", recommendations);
        }

        private ITransformer LoadModel(string modelPath)
        {
            DataViewSchema modelSchema;
            using var stream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return _mlContext.Model.Load(stream, out modelSchema);
        }

        private List<Book> LoadBooks(string filePath)
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";"
            });
            return csv.GetRecords<Book>().ToList();
        }

        private float GetUserId(string user)
        {
            return user switch
            {
                "Артур" => 1,
                "Дима" => 2,
                "Вероника" => 3,
                _ => throw new ArgumentException("Invalid user")
            };
        }

        private List<Tuple<Book, float>> GetRecommendations(float userId)
        {
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<BookRatingData, BookRatingPrediction>(_model);
            var predictions = new List<Tuple<Book, float>>();

            foreach (var book in _books)
            {
                var testData = new BookRatingData { UserId = userId, ISBN = book.ISBN };
                var prediction = predictionEngine.Predict(testData);
                predictions.Add(new Tuple<Book, float>(book, prediction.Score));
            }

            return predictions.OrderByDescending(p => p.Item2).Take(5).ToList();
        }
    }
}
