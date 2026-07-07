namespace DicomRag.Api.Models
{
    public class IngestResponse
    {
        public string ChunkId { get; set; } = string.Empty;
        public string Modality { get; set; } = string.Empty;
        public string BodyPartExamined { get; set; } = string.Empty;
        public string Message { get; set; } = "Ingested and indexed successfully.";
    }
}
