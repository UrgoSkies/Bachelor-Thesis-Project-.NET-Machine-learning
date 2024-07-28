using CsvHelper.Configuration.Attributes;

namespace BookRecommendationApp.Models
{
    public class Book
    {
        public long ISBN { get; set; }

        [Name("Book-Title")]
        public string BookTitle { get; set; }

        [Name("Book-Author")]
        public string BookAuthor { get; set; }

        public string Publisher { get; set; }

        [Name("Year-Of-Publication")]
        public int YearOfPublication { get; set; }

        [Ignore]
        public bool Liked { get; set; }
    }
}
