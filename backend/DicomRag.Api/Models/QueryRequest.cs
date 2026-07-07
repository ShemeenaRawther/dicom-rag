namespace DicomRag.Api.Models
{
    public class QueryRequest
    {
        public string Question { get; set; } = string.Empty;
        public string? ModalityFilter { get; set; }
        public string? BodyPartFilter { get; set; }
        public int TopK { get; set; } = 5;
    }
}
