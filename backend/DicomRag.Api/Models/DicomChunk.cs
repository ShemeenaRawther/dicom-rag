namespace DicomRag.Api.Models
{
    public class DicomChunk
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        // Hashed, never the raw MRN/PatientID — de-identification happens at ingestion time.
        public string PatientHash { get; set; } = string.Empty;

        public string Modality { get; set; } = string.Empty;
        public string BodyPartExamined { get; set; } = string.Empty;
        public string StudyDescription { get; set; } = string.Empty;
        public string SeriesDescription { get; set; } = string.Empty;
        public string StudyDate { get; set; } = string.Empty;

        // Free text: radiology report / findings / impression. This is what drives
        // most of the semantic retrieval quality — DICOM tags alone rarely are enough.
        public string ReportText { get; set; } = string.Empty;

        public string SourceFileName { get; set; } = string.Empty;

        /// <summary>Text that actually gets embedded — tags folded into the report text.</summary>
        public string ToEmbeddingText()
        {
            return $"Modality: {Modality}. Body part: {BodyPartExamined}. " +
                   $"Study: {StudyDescription}. Series: {SeriesDescription}. " +
                   $"Date: {StudyDate}. Findings: {ReportText}";
        }
    }
}
