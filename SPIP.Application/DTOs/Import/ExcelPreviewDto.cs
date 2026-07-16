namespace SPIP.Application.DTOs.Import;

public class ExcelPreviewDto
{
    public List<List<string>> DataGrid { get; set; } = new();
    public int TotalRowsFound { get; set; }
}
