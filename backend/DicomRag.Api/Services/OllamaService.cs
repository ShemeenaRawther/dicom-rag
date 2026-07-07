using System.Text;
using System.Text.Json;

namespace DicomRag.Api.Services
{
    public class OllamaService
    {
        private readonly HttpClient _http;
        private readonly string _embeddingModel;
        private readonly string _generationModel;

        public OllamaService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _http.BaseAddress = new Uri(config["Ollama:BaseUrl"] ?? "http://localhost:11434");
            _embeddingModel = config["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
            _generationModel = config["Ollama:GenerationModel"] ?? "llama3";
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            var payload = new { model = _embeddingModel, prompt = text };
            var response = await _http.PostAsync("/api/embeddings",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var embeddingElement = doc.RootElement.GetProperty("embedding");
            var vector = new float[embeddingElement.GetArrayLength()];
            for (var i = 0; i < vector.Length; i++)
                vector[i] = embeddingElement[i].GetSingle();

            return vector;
        }

        /// <summary>
        /// Generates an answer that is instructed to use ONLY the supplied context.
        /// This prompt is the main lever against hallucination — tighten it further
        /// (few-shot examples, stricter refusal language) before any clinical use.
        /// </summary>
        public async Task<string> GenerateGroundedAnswerAsync(string question, string context)
        {
            var prompt = $"""
            You are assisting a clinician by summarizing retrieved radiology report excerpts.
            Use ONLY the information in the context below. Do not use outside knowledge.
            If the context does not contain enough information to answer, say so explicitly —
            do not guess or fill gaps with general medical knowledge.
            Cite which excerpt(s) support each claim using their [n] label.

            Context:
            {context}

            Question: {question}

            Answer (with [n] citations):
            """;

            var payload = new { model = _generationModel, prompt, stream = false };
            var response = await _http.PostAsync("/api/generate",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("response").GetString() ?? string.Empty;
        }
    }
}
