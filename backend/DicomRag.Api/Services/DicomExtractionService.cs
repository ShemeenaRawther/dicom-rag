using DicomRag.Api.Models;
using FellowOakDicom;
using System.Security.Cryptography;
using System.Text;

namespace DicomRag.Api.Services
{
    /// <param name="stream">Raw DICOM (.dcm) file stream.</param>
    /// <param name="reportText">
    /// Free-text report/findings, supplied alongside the file. Parsing DICOM
    /// Structured Reports (SR) is possible via DicomDataset.GetSequence(DicomTag.ContentSequence)
    /// but is genuinely fiddly — most sites already have the report as a separate
    /// HL7/FHIR DiagnosticReport text, so we accept it directly here rather than
    /// forcing an SR round-trip. Swap in SR parsing when your source system needs it.
    /// </param>
    public class DicomExtractionService
    {
        public async Task<DicomChunk> ExtractAsync(Stream stream, string reportText, string sourceFileName)
        {
            var dicomFile = await DicomFile.OpenAsync(stream);
            var ds = dicomFile.Dataset;
            var rawPatientId = ds.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);

            var chunk = new DicomChunk
            {
                PatientHash = HashPatientId(rawPatientId),
                Modality = ds.GetSingleValueOrDefault(DicomTag.Modality, string.Empty),
                BodyPartExamined = ds.GetSingleValueOrDefault(DicomTag.BodyPartExamined, string.Empty),
                StudyDescription = ds.GetSingleValueOrDefault(DicomTag.StudyDescription, string.Empty),
                SeriesDescription = ds.GetSingleValueOrDefault(DicomTag.SeriesDescription, string.Empty),
                StudyDate = ds.GetSingleValueOrDefault(DicomTag.StudyDate, string.Empty),
                ReportText = reportText ?? string.Empty,
                SourceFileName = sourceFileName
            };

            return chunk;
        }

        /// <summary>
        /// One-way hash so the same patient's studies can still be grouped/filtered
        /// on the backend without ever storing or exposing the raw identifier.
        /// For production, combine with a per-deployment salt (e.g. from config/secrets),
        /// and confirm your full de-identification approach against HIPAA Safe Harbor
        /// or Expert Determination — this hash alone is not a complete de-identification policy.
        /// </summary>
        private static string HashPatientId(string rawId)
        {
            if (string.IsNullOrWhiteSpace(rawId)) return "unknown";
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawId));
            return Convert.ToHexString(bytes)[..16]; // shortened for readability in logs/UI
        }
    }
}
