const API_BASE = "http://localhost:5075/api";

export async function ingestDicom(file, reportText) {
  const form = new FormData();
  form.append("file", file);
  form.append("reportText", reportText);

  const res = await fetch(`${API_BASE}/ingest`, { method: "POST", body: form });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}

export async function queryReports({ question, modalityFilter, bodyPartFilter, topK = 5 }) {
  const res = await fetch(`${API_BASE}/query`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      question,
      modalityFilter: modalityFilter || null,
      bodyPartFilter: bodyPartFilter || null,
      topK
    })
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}
