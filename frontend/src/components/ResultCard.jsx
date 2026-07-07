export default function ResultCard({ index, source }) {
  return (
    <li className="source-card">
      <div className="source-card-header">
        <span className="source-badge">[{index}]</span>
        <span>{source.modality || "Unknown modality"}</span>
        <span className="dot">·</span>
        <span>{source.bodyPartExamined || "Unknown region"}</span>
        <span className="dot">·</span>
        <span>{source.studyDate || "Undated"}</span>
        <span className="score" title="Similarity score">
          {source.score?.toFixed(2)}
        </span>
      </div>
      <p className="source-snippet">{source.snippet}</p>
    </li>
  );
}
