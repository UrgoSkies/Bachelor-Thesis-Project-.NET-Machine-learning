using System.Collections.Generic;

namespace BookRecommendationApp.Models
{
    public static class RatingStore
    {
        public static List<BookRatingData> Ratings { get; } = new List<BookRatingData>();
    }
}
