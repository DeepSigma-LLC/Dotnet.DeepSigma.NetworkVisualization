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

type ContentType = 'json' | 'text' | 'binary-url';

interface TabSpec {
  label: string;
  contentType: ContentType;
  render: (payload: unknown) => ReactNode;
}

const TAB_CONFIG = {
  reactflow: {
    label: 'ReactFlow',
    contentType: 'json',
    render: (p) => (
      <div style={{ width: '100%', height: '100%' }}>
        <ReactFlowNetwork data={p as ReactFlowPayload} height="100%" />
      </div>
    ),
  },
  cytoscape: {
    label: 'Cytoscape.js',
    contentType: 'json',
    render: (p) => <CytoscapeNetwork data={p as CytoscapePayload} height="100%" />,
  },
  d3: {
    label: 'D3',
    contentType: 'json',
    render: (p) => <D3Network data={p as D3Payload} height={650} width={1100} />,
  },
  mermaid: {
    label: 'Mermaid',
    contentType: 'text',
    render: (p) => (
      <>
        <div className="panel-title">Rendered Mermaid (text input below)</div>
        <MermaidNetwork text={p as string} />
        <RawDetails label="raw mermaid" text={p as string} />
      </>
    ),
  },
  dot: {
    label: 'GraphViz DOT',
    contentType: 'text',
    render: (p) => (
      <>
        <div className="panel-title">Rendered GraphViz (DOT source below)</div>
        <DotNetwork text={p as string} />
        <RawDetails label="raw dot" text={p as string} />
      </>
    ),
  },
  svg: {
    label: 'SVG',
    contentType: 'text',
    render: (p) => <div className="svg-host" dangerouslySetInnerHTML={{ __html: p as string }} />,
  },
  png: {
    label: 'PNG (Skia)',
    contentType: 'binary-url',
    render: (p) => <div className="png-host"><img src={p as string} alt="rendered network" /></div>,
  },
  core: {
    label: 'Core JSON',
    contentType: 'json',
    render: (p) => <pre className="code">{JSON.stringify(p, null, 2)}</pre>,
  },
} satisfies Record<string, TabSpec>;

type TabKey = keyof typeof TAB_CONFIG;
const TAB_KEYS = Object.keys(TAB_CONFIG) as TabKey[];

function RawDetails({ label, text }: { label: string; text: string }) {
  return (
    <details style={{ marginTop: 12 }}>
      <summary style={{ cursor: 'pointer', color: '#64748b' }}>{label}</summary>
      <pre className="code" style={{ height: 220 }}>{text}</pre>
    </details>
  );
}

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
          {TAB_KEYS.map((k) => (
            <button key={k} className={`tab ${tab === k ? 'active' : ''}`} onClick={() => setTab(k)}>
              {TAB_CONFIG[k].label}
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
  const [payload, setPayload] = useState<unknown>(null);
  const [loading, setLoading] = useState(true);
  const cfg = TAB_CONFIG[tab];

  useEffect(() => {
    setLoading(true);
    setPayload(null);
    const url = `/api/samples/${sample}/${tab}`;
    const ctrl = new AbortController();

    if (cfg.contentType === 'binary-url') {
      setPayload(url);
      setLoading(false);
    } else {
      const parse = cfg.contentType === 'json'
        ? (r: Response) => r.json()
        : (r: Response) => r.text();
      fetch(url, { signal: ctrl.signal })
        .then(parse)
        .then((d) => { setPayload(d); setLoading(false); })
        .catch(() => {});
    }
    return () => ctrl.abort();
  }, [sample, tab, cfg.contentType]);

  if (loading || payload === null) return <div className="loading">Loading…</div>;
  return <>{cfg.render(payload)}</>;
}
