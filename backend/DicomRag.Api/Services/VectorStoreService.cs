using DicomRag.Api.Models;
using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DicomRag.Api.Services
{
    /// <summary>
    /// Thin client over Qdrant's REST API (default http://localhost:6333).
    /// Chosen for the local Ollama stack because it runs in one Docker container,
    /// needs no separate auth setup for local dev, and its filter syntax maps
    /// directly onto the DICOM metadata fields we filter on (modality, body part).
    /// </summary>
    public class VectorStoreService
    {
        private readonly HttpClient _http;
        private readonly string _collection;
        private readonly int _vectorSize;
        private bool _collectionEnsured;

        public VectorStoreService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _http.BaseAddress = new Uri(config["Qdrant:BaseUrl"] ?? "http://localhost:6333");
            _collection = config["Qdrant:CollectionName"] ?? "dicom_reports";
            _vectorSize = int.TryParse(config["Qdrant:VectorSize"], out var v) ? v : 768;
        }

        public async Task EnsureCollectionAsync()
        {
            if (_collectionEnsured) return;
            var existing = await _http.GetAsync($"/collections/{_collection}");
            if (existing.IsSuccessStatusCode)
            {
                _collectionEnsured = true;
                return;
            }

            var body = new
            {
                vectors = new { size = _vectorSize, distance = "Cosine" }
            };

            var resp = await _http.PutAsync($"/collections/{_collection}", JsonContent(body));
            resp.EnsureSuccessStatusCode();
            _collectionEnsured = true;
        }

        private static StringContent JsonContent(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        public async Task UpsertAsync(DicomChunk chunk, float[] vector)
        {
            await EnsureCollectionAsync();

            var points = new
            {
                points = new[]
                {
                    new
                    {
                        id = chunk.Id,
                        vector = vector,
                        payload = new
                        {
                            patientHash = chunk.PatientHash,
                            modality = chunk.Modality,
                            bodyPartExamined = chunk.BodyPartExamined,
                            studyDescription = chunk.StudyDescription,
                            seriesDescription = chunk.SeriesDescription,
                            studyDate = chunk.StudyDate,
                            reportText = chunk.ReportText,
                            sourceFileName = chunk.SourceFileName
                        }
                    }
                }
            };

            var resp = await _http.PutAsync($"/collections/{_collection}/points", JsonContent(points));
            resp.EnsureSuccessStatusCode();
        }

        public async Task<List<(SourceCitation Citation, string ReportText)>> SearchAsync(float[] queryVector, int topK, string? modalityFilter, string? bodyPartFilter)
        {
            await EnsureCollectionAsync();
            var mustConditions = new List<object>();
            if (!string.IsNullOrWhiteSpace(modalityFilter))
                mustConditions.Add(new { key = "modality", match = new { value = modalityFilter } });
            if (!string.IsNullOrWhiteSpace(bodyPartFilter))
                mustConditions.Add(new { key = "bodyPartExamined", match = new { value = bodyPartFilter } });

            var body = new Dictionary<string, object?>
            {
                ["vector"] = queryVector,
                ["limit"] = topK,
                ["with_payload"] = true

            };

            if (mustConditions.Any())
            {
                body["filter"] = new { must = mustConditions };
            }

            var resp = await _http.PostAsync($"/collections/{_collection}/points/search", JsonContent(body));
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync();
            var root = JsonNode.Parse(json)!.AsObject();
            var results = new List<(SourceCitation, string)>();
            foreach (var item in root["result"]!.AsArray())
            {
                var payload= item!["payload"]!.AsObject();
                var reportText = payload["reportText"]!.GetValue<string>();

                var citation = new SourceCitation
                {
                    ChunkId = item["id"]!.GetValue<string>(),
                    Modality = payload["modality"]!.GetValue<string>(),
                    BodyPartExamined = payload["bodyPartExamined"]!.GetValue<string>(),
                    StudyDate = payload["studyDate"]!.GetValue<string>(),
                    Snippet = reportText.Length > 200 ? reportText.Substring(0, 200) + "..." : reportText,
                    Score = item["score"]!.GetValue<float>()
                };
            }
            return results;
        }
    }
}