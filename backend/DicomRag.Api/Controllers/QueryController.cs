using DicomRag.Api.Models;
using DicomRag.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DicomRag.Api.Controllers
{
    [Route("api/query")]
    [ApiController]
    public class QueryController : ControllerBase
    {
        private readonly OllamaService _ollama;
        private readonly VectorStoreService _vectorStore;
        private readonly ILogger<QueryController> _logger;

        // Below this similarity score, the retrieved context is treated as too
        // weak to answer from — the API returns an explicit low-confidence flag
        // instead of letting the LLM improvise. Tune against your own embedding
        // model's score distribution.
        private const float MinConfidenceScore = 0.55f;
        public QueryController(OllamaService ollama, VectorStoreService vectorStore, ILogger<QueryController> logger)
        {
            _ollama = ollama;
            _vectorStore = vectorStore;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<QueryResponse>> Query([FromBody] QueryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest("Question is required.");

            var queryVector = await _ollama.GetEmbeddingAsync(request.Question);
            var results = await _vectorStore.SearchAsync(
            queryVector, request.TopK, request.ModalityFilter, request.BodyPartFilter);

            if (results.Count == 0 || results.All(r => r.Citation.Score < MinConfidenceScore))
            {
                return Ok(new QueryResponse
                {
                    Answer = "No sufficiently similar reports were found to answer this question. " +
                             "Try broadening filters or rephrasing.",
                    Sources = new List<SourceCitation>(),
                    LowConfidence = true
                });
            }

            var context = string.Join("\n\n", results.Select((r, i) =>
           $"[{i + 1}] (modality={r.Citation.Modality}, bodyPart={r.Citation.BodyPartExamined}, date={r.Citation.StudyDate})\n{r.ReportText}"));

            var answer = await _ollama.GenerateGroundedAnswerAsync(request.Question, context);

            _logger.LogInformation("Query answered using {Count} retrieved chunks", results.Count);

            return Ok(new QueryResponse
            {
                Answer = answer,
                Sources = results.Select(r => r.Citation).ToList(),
                LowConfidence = false
            });
        }
    }
}
