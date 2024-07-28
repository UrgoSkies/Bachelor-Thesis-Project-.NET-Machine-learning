using CsvHelper.Configuration.Attributes;

namespace BookRecommendationApp.Models
{
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
}
