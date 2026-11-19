using CsvHelper;

namespace CSV_File_Translate.Models;

[CsvRecord]
public partial class CsvRow
{
    [CsvColumn("key")]
    public required string Key { get; set; }
    
    [CsvColumn("source")]
    public required string Source { get; set; }
    
    [CsvColumn("target")]
    public string? Target { get; set; }
}