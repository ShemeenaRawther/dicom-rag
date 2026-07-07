import UploadPanel from "./components/UploadPanel.jsx";
import QueryPanel from "./components/QueryPanel.jsx";

export default function App() {
  return (
    <div className="app-shell">
      <header className="app-header">
        <h1>DICOM RAG console</h1>
        <p className="hint">
          Local stack: .NET API · Ollama (embeddings + generation) · Qdrant (vector store)
        </p>
      </header>
      <main className="app-grid">
        <UploadPanel />
        <QueryPanel />
      </main>
    </div>
  );
}
