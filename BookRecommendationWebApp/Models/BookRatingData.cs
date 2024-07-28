using Microsoft.ML.Data;

public class BookRatingData
{
    [LoadColumn(0)]
    public float UserId { get; set; }

    [LoadColumn(1)]
    public float ISBN { get; set; }

    [LoadColumn(2)]
    public float Label { get; set; } // Рейтинг книги

    [LoadColumn(3)]
    public string BookTitle { get; set; } // Название книги (если она есть в данных)
}
