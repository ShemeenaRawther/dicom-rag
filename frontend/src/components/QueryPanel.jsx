import { useState } from "react";
import { queryReports } from "../api.js";
import ResultCard from "./ResultCard.jsx";

export default function QueryPanel() {
  const [question, setQuestion] = useState("");
  const [modalityFilter, setModalityFilter] = useState("");
  const [bodyPartFilter, setBodyPartFilter] = useState("");
  const [result, setResult] = useState(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(null);

  async function handleSubmit(e) {
    e.preventDefault();
    if (!question.trim()) return;
    setBusy(true);
    setError(null);
    setResult(null);
    try {
      const data = await queryReports({ question, modalityFilter, bodyPartFilter });
      setResult(data);
    } catch (err) {
      setError(err.message || "Query failed.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="panel">
      <h2>Ask about indexed studies</h2>
      <form onSubmit={handleSubmit} className="stack">
        <label className="field">
          <span>Question</span>
          <textarea
            rows={3}
            value={question}
            onChange={(e) => setQuestion(e.target.value)}
            placeholder="Have any prior chest CTs shown a nodule in the right lower lobe?"
          />
        </label>

        <div className="filter-row">
          <label className="field">
            <span>Modality filter (optional)</span>
            <input
              value={modalityFilter}
              onChange={(e) => setModalityFilter(e.target.value)}
              placeholder="CT"
            />
          </label>
          <label className="field">
            <span>Body part filter (optional)</span>
            <input
              value={bodyPartFilter}
              onChange={(e) => setBodyPartFilter(e.target.value)}
              placeholder="CHEST"
            />
          </label>
        </div>

        <button type="submit" disabled={busy}>
          {busy ? "Searching…" : "Ask"}
        </button>
      </form>

      {error && <p className="status status-error">{error}</p>}

      {result && (
        <div className="answer-block">
          {result.lowConfidence && (
            <p className="status status-warning">
              Low-confidence retrieval — treat this answer with extra caution.
            </p>
          )}
          <h3>Answer</h3>
          <p className="answer-text">{result.answer}</p>

          {result.sources?.length > 0 && (
            <>
              <h3>Sources</h3>
              <ul className="source-list">
                {result.sources.map((s, i) => (
                  <ResultCard key={s.chunkId} index={i + 1} source={s} />
                ))}
              </ul>
            </>
          )}
        </div>
      )}
    </section>
  );
}
