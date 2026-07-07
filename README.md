# DICOM RAG — .NET + React + Ollama + Qdrant

A working skeleton for the pipeline discussed earlier: DICOM ingestion →
de-identification → embedding → vector retrieval → grounded LLM generation
with citations.

## Stack

| Layer | Tech | Why |
|---|---|---|
| Backend API | ASP.NET Core 8 | as requested |
| DICOM parsing | fo-dicom | mature .NET DICOM library (tags, pixel data) |
| Frontend | React + Vite | as requested |
| Embeddings + generation | Ollama (local) | as requested — runs `nomic-embed-text` + `llama3` locally |
| Vector store | Qdrant | pairs naturally with a local Ollama stack — one Docker container, REST API, native metadata filtering (modality/body part), no cloud account needed |

You can swap Qdrant for pgvector, Milvus, or Weaviate later — `VectorStoreService.cs`
is the only file that would need to change, since it's the sole thing talking to it.

## What's implemented

- **Ingestion** (`IngestController` → `DicomExtractionService`): reads a `.dcm` file,
  extracts modality / body part / study & series description / study date, hashes
  the PatientID (never stores the raw MRN), and accepts report/findings text
  alongside the file.
- **Embedding + indexing** (`OllamaService` + `VectorStoreService`): embeds the
  combined metadata + report text via Ollama, upserts into Qdrant with metadata
  as a filterable payload.
- **Retrieval + generation** (`QueryController`): embeds the question, does a
  filtered vector search, builds a numbered context block, and asks the LLM to
  answer **only** from that context with `[n]` citations. Returns a low-confidence
  flag if nothing scores above threshold, instead of letting the model guess.
- **Frontend**: an upload panel and a query panel with modality/body-part filters,
  answer display, and a citation list showing which retrieved report supported
  which claim.

## What's intentionally left for you to extend

- **DICOM Structured Report (SR) parsing** — this build accepts report text as a
  separate field rather than walking the SR `ContentSequence` tree, since most
  real deployments already have report text from an HL7/FHIR feed. If you need
  to parse SR directly, that's isolated to `DicomExtractionService.ExtractAsync`.
- **Image embeddings** — only text is embedded here. To add image-based
  retrieval, extract pixel data with fo-dicom's `DicomImage`, run it through a
  medical vision-language model (BiomedCLIP etc. — Ollama doesn't currently
  serve these, so this would call a separate Python microservice), and store a
  second vector alongside the text one.
- **Auth, audit logging, encryption at rest** — required for any real clinical
  deployment; not in this skeleton.
- **Batch ingestion** — currently one file per request; wrap `IngestController`
  in a loop or a background job for whole studies/series.

## Running it

### 1. Start Qdrant + Ollama

```bash
docker compose up -d
```

### 2. Pull the Ollama models

```bash
docker exec -it dicom-rag-ollama ollama pull nomic-embed-text
docker exec -it dicom-rag-ollama ollama pull llama3
```

`nomic-embed-text` produces 768-dim vectors, matching `Qdrant:VectorSize` in
`appsettings.json`. If you switch embedding models, update that value to match
— Qdrant collections are fixed-dimension.

### 3. Run the backend

```bash
cd backend/DicomRag.Api
dotnet restore
dotnet run
```

API comes up at `http://localhost:5000` with Swagger at `/swagger`.

### 4. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

App comes up at `http://localhost:5173`.

## Trying it out

1. Ingest: pick any `.dcm` file, paste in a short findings paragraph, submit.
2. Ask a question about it — try filtering by modality (e.g. `CT`) or body
   part (e.g. `CHEST`) to see the metadata filter combine with semantic search.
3. Check the citations — the answer is generated only from the retrieved
   excerpts shown below it.

## Before this touches real patient data

- Confirm your de-identification approach meets HIPAA Safe Harbor or Expert
  Determination — the SHA-256 hash here is a starting point, not a compliance
  sign-off.
- Add a per-deployment salt to the patient ID hash (currently unsalted).
- Put Qdrant and Ollama behind your network boundary — both are unauthenticated
  by default in this compose file.
- If outputs could influence diagnosis or treatment, check whether this falls
  under FDA Software as a Medical Device (SaMD) rules before deployment.
