using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;

public class MatrixFactorization
{
    public static void Main(string[] args)
    {
        string dataPath = "C:\\Users\\Rinald\\Desktop\\Data-cleared\\rates.csv"; 
        var data = ReadDataFromCsv(dataPath);

        // writing unique values to the list
        var users = data.Select(d => d.user_id).Distinct().ToList();
        var books = data.Select(d => d.book_id).Distinct().ToList();

        // creating a rating matrix
        var ratingsMatrix = new float[users.Count, books.Count];
        foreach (var (user_id, book_id, rating) in data)
        {
            ratingsMatrix[users.IndexOf(user_id), books.IndexOf(book_id)] = rating;
        }

        Console.WriteLine("Initial Ratings Matrix:");
        PrintMatrix(ratingsMatrix, users.Count, books.Count);

        // matrix factors
        int k = 2; // users and books  
        var userFactors = new float[users.Count, k];
        var bookFactors = new float[books.Count, k];
        var rand = new Random();

        // random 
        for (int i = 0; i < users.Count; i++)
            for (int j = 0; j < k; j++)
                userFactors[i, j] = (float)rand.NextDouble();

        for (int i = 0; i < books.Count; i++)
            for (int j = 0; j < k; j++)
                bookFactors[i, j] = (float)rand.NextDouble();

        Console.WriteLine("Initial User Factors:");
        PrintMatrix(userFactors, users.Count, k);

        Console.WriteLine("Initial Book Factors:");
        PrintMatrix(bookFactors, books.Count, k);

        // algorithm parameters
        int iterations = 500;
        float alpha = 0.01f;
        float lambda = 0.02f;

        // ALS (Alternating Least Squares) algorithm
        for (int iter = 0; iter < iterations; iter++)
        {
            for (int u = 0; u < users.Count; u++)
            {
                for (int i = 0; i < books.Count; i++)
                {
                    if (ratingsMatrix[u, i] > 0)
                    {
                        float error = ratingsMatrix[u, i] - PredictRating(userFactors, bookFactors, u, i);
                        for (int f = 0; f < k; f++)
                        {
                            userFactors[u, f] += alpha * (2 * error * bookFactors[i, f] - lambda * userFactors[u, f]);
                            bookFactors[i, f] += alpha * (2 * error * userFactors[u, f] - lambda * bookFactors[i, f]);
                        }
                    }
                }
            }
        }

        Console.WriteLine("Final User Factors:");
        PrintMatrix(userFactors, users.Count, k);

        Console.WriteLine("Final Book Factors:");
        PrintMatrix(bookFactors, books.Count, k);

        // rating prediction for user_id = 2 and book_id = 5
        int targetUserIndex = users.IndexOf(2);
        int targetBookIndex = books.IndexOf(5);
        float predictedRating = PredictRating(userFactors, bookFactors, targetUserIndex, targetBookIndex);

        Console.WriteLine($"Predicted rating for user 2 on book 5: {predictedRating}");
    }

    // rating prediction function
    private static float PredictRating(float[,] userFactors, float[,] bookFactors, int userIndex, int bookIndex)
    {
        float prediction = 0;
        for (int f = 0; f < userFactors.GetLength(1); f++)
        {
            prediction += userFactors[userIndex, f] * bookFactors[bookIndex, f];
        }
        return prediction;
    }

    // reading data from CSV file
    private static List<(int user_id, int book_id, float rating)> ReadDataFromCsv(string filePath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HasHeaderRecord = true
        };

        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, config))
        {
            var records = csv.GetRecords<BookRatingData>().ToList();
            return records.Select(r => (r.user_id, r.book_id, r.rating)).ToList();
        }
    }

    // class for presenting data from CSV file
    public class BookRatingData
    {
        public int book_id { get; set; }
        public int user_id { get; set; }
        public float rating { get; set; }
    }

    // matrix printing
    private static void PrintMatrix(float[,] matrix, int rows, int cols)
    {
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                Console.Write($"{matrix[i, j]:F2} ");
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }
}
