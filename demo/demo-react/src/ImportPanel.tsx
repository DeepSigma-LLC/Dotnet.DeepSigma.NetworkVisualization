import { useState } from 'react';

const FORMATS = [
  {
    key: 'json',
    label: 'JSON (Core)',
    contentType: 'application/json',
    example: `{
  "format": "deepsigma.network",
  "version": "1.0",
  "network": {
    "directed": true,
    "nodes": [{ "id": "a", "label": "Alice" }, { "id": "b", "label": "Bob" }],
    "edges": [{ "id": "e1", "source": "a", "target": "b", "label": "knows" }]
  }
}`,
  },
  {
    key: 'csv',
    label: 'CSV',
    contentType: 'text/csv',
    example: `## nodes
id,label,color
a,Alice,#FF0000
b,Bob,#00FF00
## edges
source,target,label
a,b,knows`,
  },
] as const;

type FormatKey = typeof FORMATS[number]['key'];

export interface ImportPanelProps {
  onImported: (id: string) => void;
}

export function ImportPanel({ onImported }: ImportPanelProps) {
  const [open, setOpen] = useState(false);
  const [format, setFormat] = useState<FormatKey>('json');
  const [title, setTitle] = useState('');
  const [body, setBody] = useState('');
  const [status, setStatus] = useState<string>('');
  const [busy, setBusy] = useState(false);

  const current = FORMATS.find((f) => f.key === format)!;

  const submit = async () => {
    if (!body.trim()) { setStatus('Paste a payload first.'); return; }
    setBusy(true);
    setStatus('Importing…');
    const qs = new URLSearchParams({ format });
    if (title.trim()) qs.set('title', title.trim());
    const resp = await fetch(`/api/import?${qs}`, {
      method: 'POST',
      headers: { 'Content-Type': current.contentType },
      body,
    });
    const result = await resp.json();
    if (resp.ok) {
      setStatus(`Imported ${result.nodeCount} nodes · ${result.edgeCount} edges`);
      setBody('');
      onImported(result.id);
      setTimeout(() => { setStatus(''); setOpen(false); }, 1500);
    } else {
      setStatus(`Error: ${(result.error as string) ?? 'failed'}`);
    }
    setBusy(false);
  };

  if (!open) {
    return <button className="import-trigger" onClick={() => setOpen(true)}>+ Import network…</button>;
  }
  return (
    <div className="import-form">
      <label>Format
        <select value={format} onChange={(e) => setFormat(e.target.value as FormatKey)} disabled={busy}>
          {FORMATS.map((f) => <option key={f.key} value={f.key}>{f.label}</option>)}
        </select>
      </label>
      <label>Title (optional)
        <input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="my network" disabled={busy} />
      </label>
      <textarea
        value={body}
        onChange={(e) => setBody(e.target.value)}
        placeholder={current.example}
        disabled={busy}
        rows={10}
      />
      <div className="import-actions">
        <button onClick={submit} disabled={busy} className="primary">Import</button>
        <button onClick={() => { setOpen(false); setBody(''); setStatus(''); }} disabled={busy}>Cancel</button>
      </div>
      {status && <div className="import-status">{status}</div>}
    </div>
  );
}
