namespace DicomRag.Api.Models
{
    public class QueryResponse
    {
        public string Answer { get; set; } = string.Empty;
        public List<SourceCitation> Sources { get; set; } = new();
        public bool LowConfidence { get; set; }
    }
}
