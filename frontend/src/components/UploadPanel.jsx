import { useState } from "react";
import { ingestDicom } from "../api.js";

export default function UploadPanel() {
  const [file, setFile] = useState(null);
  const [reportText, setReportText] = useState("");
  const [status, setStatus] = useState(null);
  const [busy, setBusy] = useState(false);

  async function handleSubmit(e) {
    e.preventDefault();
    if (!file) {
      setStatus({ type: "error", message: "Choose a .dcm file first." });
      return;
    }
    setBusy(true);
    setStatus(null);
    try {
      const result = await ingestDicom(file, reportText);
      setStatus({
        type: "success",
        message: `Indexed ${result.modality || "study"} / ${result.bodyPartExamined || "unknown region"} as ${result.chunkId}`
      });
      setFile(null);
      setReportText("");
    } catch (err) {
      setStatus({ type: "error", message: err.message || "Ingestion failed." });
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="panel">
      <h2>Ingest a study</h2>
      <p className="hint">
        Upload a DICOM file and its report text. Patient identifiers are hashed
        before anything is stored or embedded.
      </p>
      <form onSubmit={handleSubmit} className="stack">
        <label className="field">
          <span>DICOM file (.dcm)</span>
          <input
            type="file"
            accept=".dcm"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
          />
        </label>

        <label className="field">
          <span>Report / findings text</span>
          <textarea
            rows={5}
            value={reportText}
            onChange={(e) => setReportText(e.target.value)}
            placeholder="Impression: no acute findings. Mild degenerative changes..."
          />
        </label>

        <button type="submit" disabled={busy}>
          {busy ? "Indexing…" : "Ingest study"}
        </button>
      </form>

      {status && (
        <p className={status.type === "error" ? "status status-error" : "status status-success"}>
          {status.message}
        </p>
      )}
    </section>
  );
}
