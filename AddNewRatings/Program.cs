using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

class Program
{
    static void Main(string[] args)
    {
        string inputFilePath = @"C:\Users\Rinald\Desktop\booktest.csv";
        string outputFilePath = @"C:\Users\Rinald\Desktop\booktestadded.csv";

        var records = ReadDataFromCsv(inputFilePath);
        Console.WriteLine($"Loaded {records.Count} records from CSV file.");

        var updatedRecords = AddMissingRatings(records);

        WriteDataToCsv(outputFilePath, updatedRecords);
        Console.WriteLine("Updated CSV file saved.");

        Console.WriteLine("Process completed.");
    }

    private static List<BookRatingData> ReadDataFromCsv(string filePath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ","
        };

        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, config))
        {
            return csv.GetRecords<BookRatingData>().ToList();
        }
    }

    private static List<BookRatingData> AddMissingRatings(List<BookRatingData> records)
    {
        var allISBNs = records.Select(r => r.ISBN).Distinct().ToList();
        var allUsers = records.Select(r => r.UserID).Distinct().OrderBy(userId => userId).ToList();

        var updatedRecords = new List<BookRatingData>(records);

        foreach (var user in allUsers)
        {
            var userRecords = records.Where(r => r.UserID == user).Select(r => r.ISBN).ToList();
            var missingISBNs = allISBNs.Except(userRecords).ToList();

            foreach (var isbn in missingISBNs)
            {
                updatedRecords.Add(new BookRatingData
                {
                    ISBN = isbn,
                    UserID = user,
                    BookRating = 0
                });
            }

            Console.WriteLine($"Processed user {user}. Added {missingISBNs.Count} missing ratings.");
        }

        return updatedRecords.OrderBy(r => r.UserID).ThenBy(r => r.ISBN).ToList();
    }

    private static void WriteDataToCsv(string filePath, List<BookRatingData> records)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HasHeaderRecord = true
        };

        using (var writer = new StreamWriter(filePath))
        using (var csv = new CsvWriter(writer, config))
        {
            // Сортировка по BookRating перед записью
            var sortedRecords = records.OrderBy(r => r.BookRating).ThenBy(r => r.UserID).ThenBy(r => r.ISBN).ToList();

            csv.WriteRecords(sortedRecords);
        }
    }
}

public class BookRatingData
{
    [Name("ISBN")]
    public long ISBN { get; set; }

    [Name("UserID")]
    public int UserID { get; set; }

    [Name("BookRating")]
    public int BookRating { get; set; }
}


