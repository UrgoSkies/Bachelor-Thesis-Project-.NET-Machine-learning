using System.Collections.Generic;

namespace BookRecommendationApp.Models
{
    public class UserBooksViewModel
    {
        public float UserId { get; set; }
        public List<Book> Books { get; set; }
    }
}
