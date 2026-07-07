namespace DicomRag.Api.Models
{
    public class SourceCitation
    {
        public string ChunkId { get; set; } = string.Empty;
        public string Modality { get; set; } = string.Empty;
        public string BodyPartExamined { get; set; } = string.Empty;
        public string StudyDate { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public float Score { get; set; }
    }
}
