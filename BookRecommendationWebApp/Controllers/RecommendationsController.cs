using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private static List<BookRatingData> Ratings = new List<BookRatingData>();
    private readonly PredictionService _predictionService;

    public RecommendationsController(PredictionService predictionService)
    {
        _predictionService = predictionService;

        // Инициализация начальных данных
        if (!Ratings.Any())
        {
            InitializeRatings();
        }
    }

    [HttpPost("rate")]
    public IActionResult RateBooks([FromBody] List<BookRatingData> ratings)
    {
        Ratings.AddRange(ratings);
        return Ok();
    }

    [HttpPost("recommend")]
    public IActionResult GetRecommendations([FromBody] UserRatingsRequest request)
    {
        var recommendations = _predictionService.GetRecommendationsForUser(request.UserId, Ratings);
        return Ok(recommendations);
    }

    private void InitializeRatings()
    {
        Ratings = new List<BookRatingData>
        {
            new BookRatingData { UserId = 1, ISBN = 1, Label = 10, BookTitle = "Book A" },
            new BookRatingData { UserId = 1, ISBN = 2, Label = 10, BookTitle = "Book B" },
            new BookRatingData { UserId = 2, ISBN = 2, Label = 10, BookTitle = "Book B" },
            new BookRatingData { UserId = 2, ISBN = 3, Label = 10, BookTitle = "Book C" },
            new BookRatingData { UserId = 3, ISBN = 4, Label = 0, BookTitle = "Book D" },
            new BookRatingData { UserId = 3, ISBN = 5, Label = 0, BookTitle = "Book E" }
        };
    }
}

public class UserRatingsRequest
{
    public float UserId { get; set; }
    public List<BookRatingData> Ratings { get; set; }
}
