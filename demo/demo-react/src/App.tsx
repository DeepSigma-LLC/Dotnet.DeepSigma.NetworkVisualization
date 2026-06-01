import { Component, useEffect, useState, type ReactNode } from 'react';
import {
  ReactFlowNetwork,
  CytoscapeNetwork,
  D3Network,
  MermaidNetwork,
  DotNetwork,
} from 'deepsigma-network-react';
import type { ReactFlowPayload, CytoscapePayload, D3Payload } from 'deepsigma-network-core';

interface SampleInfo { name: string; title: string; nodeCount: number; edgeCount: number; }

type TabKey = 'reactflow' | 'cytoscape' | 'd3' | 'mermaid' | 'dot' | 'svg' | 'png' | 'core';

const TABS: { key: TabKey; label: string }[] = [
  { key: 'reactflow', label: 'ReactFlow' },
  { key: 'cytoscape', label: 'Cytoscape.js' },
  { key: 'd3',        label: 'D3' },
  { key: 'mermaid',   label: 'Mermaid' },
  { key: 'dot',       label: 'GraphViz DOT' },
  { key: 'svg',       label: 'SVG' },
  { key: 'png',       label: 'PNG (Skia)' },
  { key: 'core',      label: 'Core JSON' },
];

class PanelErrorBoundary extends Component<{ resetKey: string; children: ReactNode }, { err: Error | null }> {
  state = { err: null as Error | null };
  static getDerivedStateFromError(err: Error) { return { err }; }
  componentDidUpdate(prev: { resetKey: string }) {
    if (prev.resetKey !== this.props.resetKey && this.state.err) this.setState({ err: null });
  }
  render() {
    if (this.state.err) {
      return (
        <div style={{ padding: 24, background: '#fef2f2', border: '1px solid #fecaca', borderRadius: 8, color: '#7f1d1d' }}>
          <strong>Renderer error</strong>
          <pre style={{ marginTop: 12, whiteSpace: 'pre-wrap', fontSize: 12 }}>{this.state.err.message}</pre>
        </div>
      );
    }
    return this.props.children;
  }
}

export function App() {
  const [samples, setSamples] = useState<SampleInfo[]>([]);
  const [selected, setSelected] = useState<string>('');
  const [tab, setTab] = useState<TabKey>('reactflow');

  useEffect(() => {
    fetch('/api/samples').then((r) => r.json()).then((data: SampleInfo[]) => {
      setSamples(data);
      if (data.length > 0 && !selected) setSelected(data[0]!.name);
    });
  }, []);

  return (
    <div className="layout">
      <aside className="sidebar">
        <h1>DeepSigma<br/>Networks</h1>
        <h2>Samples</h2>
        <ul className="sample-list">
          {samples.map((s) => (
            <li key={s.name} className={s.name === selected ? 'active' : ''} onClick={() => setSelected(s.name)}>
              <div>{s.title}</div>
              <div className="sample-meta">{s.nodeCount} nodes · {s.edgeCount} edges</div>
            </li>
          ))}
        </ul>
        <h2>About</h2>
        <p style={{ fontSize: 12, color: '#94a3b8', lineHeight: 1.5 }}>
          One Network model, rendered every way. Backend emits JSON / SVG / PNG / DOT / Mermaid from C#; this page mounts the JSON into ReactFlow, Cytoscape.js, and D3.
        </p>
      </aside>
      <main className="main">
        <div className="tabs">
          {TABS.map((t) => (
            <button key={t.key} className={`tab ${tab === t.key ? 'active' : ''}`} onClick={() => setTab(t.key)}>
              {t.label}
            </button>
          ))}
        </div>
        <div className="panel">
          {selected && (
            <PanelErrorBoundary resetKey={`${selected}-${tab}`}>
              <Panel key={`${selected}-${tab}`} sample={selected} tab={tab} />
            </PanelErrorBoundary>
          )}
        </div>
      </main>
    </div>
  );
}

function Panel({ sample, tab }: { sample: string; tab: TabKey }) {
  const [payload, setPayload] = useState<string | object | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    setPayload(null);
    const url = `/api/samples/${sample}/${tab}`;
    const isJson = tab === 'reactflow' || tab === 'cytoscape' || tab === 'd3' || tab === 'core';
    const isText = tab === 'mermaid' || tab === 'dot' || tab === 'svg';
    const isBinary = tab === 'png';

    const ctrl = new AbortController();
    if (isJson) {
      fetch(url, { signal: ctrl.signal }).then((r) => r.json())
        .then((d) => { setPayload(d); setLoading(false); }).catch(() => {});
    } else if (isText) {
      fetch(url, { signal: ctrl.signal }).then((r) => r.text())
        .then((d) => { setPayload(d); setLoading(false); }).catch(() => {});
    } else if (isBinary) {
      setPayload(url);
      setLoading(false);
    }
    return () => ctrl.abort();
  }, [sample, tab]);

  if (loading || payload === null) return <div className="loading">Loading…</div>;

  switch (tab) {
    case 'reactflow':
      return <div style={{ width: '100%', height: '100%' }}><ReactFlowNetwork data={payload as ReactFlowPayload} height="100%" /></div>;
    case 'cytoscape':
      return <CytoscapeNetwork data={payload as CytoscapePayload} height="100%" />;
    case 'd3':
      return <D3Network data={payload as D3Payload} height={650} width={1100} />;
    case 'mermaid':
      return (
        <>
          <div className="panel-title">Rendered Mermaid (text input below)</div>
          <MermaidNetwork text={payload as string} />
          <details style={{ marginTop: 12 }}>
            <summary style={{ cursor: 'pointer', color: '#64748b' }}>raw mermaid</summary>
            <pre className="code" style={{ height: 220 }}>{payload as string}</pre>
          </details>
        </>
      );
    case 'svg':
      return <div className="svg-host" dangerouslySetInnerHTML={{ __html: payload as string }} />;
    case 'png':
      return <div className="png-host"><img src={payload as string} alt="rendered network" /></div>;
    case 'dot':
      return (
        <>
          <div className="panel-title">Rendered GraphViz (DOT source below)</div>
          <DotNetwork text={payload as string} />
          <details style={{ marginTop: 12 }}>
            <summary style={{ cursor: 'pointer', color: '#64748b' }}>raw dot</summary>
            <pre className="code" style={{ height: 220 }}>{payload as string}</pre>
          </details>
        </>
      );
    case 'core':
      return <pre className="code">{JSON.stringify(payload, null, 2)}</pre>;
  }
}
