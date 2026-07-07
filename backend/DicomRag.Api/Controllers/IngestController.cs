using DicomRag.Api.Models;
using DicomRag.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DicomRag.Api.Controllers
{
    [Route("api/ingest")]
    [ApiController]
    public class IngestController : ControllerBase
    {
        private readonly DicomExtractionService _extraction;
        private readonly OllamaService _ollama;
        private readonly VectorStoreService _vectorStore;
        private readonly ILogger<IngestController> _logger;
        public IngestController(
            DicomExtractionService extraction,
            OllamaService ollama,
            VectorStoreService vectorStore,
            ILogger<IngestController> logger)
        {
            _extraction = extraction;
            _ollama =ollama;
            _vectorStore = vectorStore;
            _logger = logger;
        }

        /// <summary>
        /// Accepts a single DICOM file plus its report text, de-identifies,
        /// embeds, and indexes it. Multipart form: "file" + "reportText".
        /// </summary>
        [HttpPost]
        [RequestSizeLimit(200_000_000)]
        public async Task<ActionResult<IngestResponse>> Ingest(
            [FromForm] IFormFile file,
            [FromForm] string reportText)
        {
            if (file is null || file.Length == 0)
                return BadRequest("A DICOM file is required.");

            DicomChunk chunk;
            using (var stream = file.OpenReadStream())
            {
                chunk = await _extraction.ExtractAsync(stream, reportText ?? string.Empty, file.FileName);
            }

            var embeddingText = chunk.ToEmbeddingText();
            var vector = await _ollama.GetEmbeddingAsync(embeddingText);
            await _vectorStore.UpsertAsync(chunk, vector);

            _logger.LogInformation("Ingested chunk {ChunkId} (patientHash={PatientHash})",
            chunk.Id, chunk.PatientHash);

            return Ok(new IngestResponse
            {
                ChunkId = chunk.Id,
                Modality = chunk.Modality,
                BodyPartExamined = chunk.BodyPartExamined
            });

        }
    }
}