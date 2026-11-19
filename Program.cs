using CsvHelper;
using DotNetEnv;
using Google.GenAI;
using CsvRow = CSV_File_Translate.Models.CsvRow;

Env.TraversePath().Load();

var apiKey = Environment.GetEnvironmentVariable("APIKEY");

Client client = new Client(apiKey: apiKey);

async Task CsvTranslate()
{
    string? csvFilesPath = Environment.GetEnvironmentVariable("PATH");
    if (csvFilesPath != null)
    {
        var csvFiles = Directory.GetFiles(csvFilesPath, "*.csv");
        string progressFilePath = Path.Combine(csvFilesPath, "progress.txt");
        var progressFile = File.Exists(progressFilePath) ? File.ReadAllText(progressFilePath) : "";

        foreach (var csvFile in csvFiles)
        {
            if (progressFile.Contains(Path.GetFileName(csvFile)) || csvFile.Contains("_RO.csv"))
            {
                continue;
            }
        
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine($"\nWorking on {csvFile}\n");
            Console.ResetColor();
        
            await using var reader = CsvReader.FromFile(csvFile);
            var records = reader.GetRecords<CsvRow>().ToList();

            for (var i = 0 ; i < records.Count; i+= 50)
            {
                var batchObjects = records.Skip(i).Take(50).ToList();
                var englishTexts = batchObjects.Select(r => r.Source).ToArray();

                try
                {
                    var translatedTexts = await Translate(englishTexts);

                    for (var j = 0; j < batchObjects.Count; j++)
                    {
                        if (translatedTexts != null && j < translatedTexts.Length)
                        {
                            batchObjects[j].Target = translatedTexts[j];
                        }
                    }

                    Console.WriteLine($"Processed {i + batchObjects.Count} lines out of {records.Count}");
                    Thread.Sleep(8000);
                }
                catch (Exception)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed Translation Retrying..");
                    i -= 50;
                    Console.ResetColor();
                    Thread.Sleep(8000);
                }
            }

            string outputFile = csvFile.Replace(".csv", "_RO.csv");
            await using var writer = CsvWriter.ToFile(outputFile);
            File.AppendAllText(progressFilePath, Path.GetFileName(csvFile) + Environment.NewLine);
            writer.WriteRecords(records);
            Console.WriteLine($"\nTranslated {csvFile}, {csvFiles.Length - csvFiles.Count(f => !Path.GetFileName(f).Contains("_RO.csv"))} remaining..\n");
        }
    }

    Console.ForegroundColor = ConsoleColor.DarkGreen;
    Console.WriteLine("Finished Translation!");
    Console.ResetColor();
}

async Task<string[]?> Translate(string[] batch)
{
    var batchJson = System.Text.Json.JsonSerializer.Serialize(batch);
    var prompt = "You are a professional game translator. " +
                 "Translate the following JSON array of strings into Romanian. " +
                 "Keep the exact same number of items. " +
                 "Return ONLY a valid JSON array of strings, nothing else. " +
                 $"Input: {batchJson}";
    
    var response = await client.Models.GenerateContentAsync(
        model: "gemini-3.5-flash-lite",
        contents: prompt
    );

    var textResponse = response.Candidates?[0].Content?.Parts?[0].Text;

    if (textResponse != null)
    {
        return System.Text.Json.JsonSerializer.Deserialize<string[]>(textResponse);
    }

    return [];
}

await CsvTranslate();