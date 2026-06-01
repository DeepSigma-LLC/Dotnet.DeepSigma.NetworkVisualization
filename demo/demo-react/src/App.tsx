import { Component, useCallback, useEffect, useState, type ReactNode } from 'react';
import {
  ReactFlowNetwork,
  CytoscapeNetwork,
  D3Network,
  MermaidNetwork,
  DotNetwork,
  SigmaNetwork,
} from 'deepsigma-network-react';
import type {
  ReactFlowPayload,
  CytoscapePayload,
  D3Payload,
  SigmaPayload,
  NetworkEventHandlers,
} from 'deepsigma-network-core';
import { NetworkEditor } from './NetworkEditor';
import { ImportPanel } from './ImportPanel';

interface SampleInfo { name: string; title: string; nodeCount: number; edgeCount: number; edited?: boolean; imported?: boolean; }

type ContentType = 'json' | 'text' | 'binary-url';
type ThemeKey = 'light' | 'dark';

interface SelectedItem {
  kind: 'node' | 'edge';
  id: string;
  data?: Record<string, unknown>;
}

interface TabSpec {
  label: string;
  contentType: ContentType;
  render: (payload: unknown, handlers: NetworkEventHandlers) => ReactNode;
  interactive: boolean;
}

const TAB_CONFIG = {
  reactflow: {
    label: 'ReactFlow', contentType: 'json', interactive: true,
    render: (p, h) => (
      <div style={{ width: '100%', height: '100%' }}>
        <ReactFlowNetwork data={p as ReactFlowPayload} height="100%" {...h} />
      </div>
    ),
  },
  cytoscape: {
    label: 'Cytoscape.js', contentType: 'json', interactive: true,
    render: (p, h) => <CytoscapeNetwork data={p as CytoscapePayload} height="100%" {...h} />,
  },
  d3: {
    label: 'D3', contentType: 'json', interactive: true,
    render: (p, h) => <D3Network data={p as D3Payload} height={650} width={1100} {...h} />,
  },
  sigma: {
    label: 'Sigma.js', contentType: 'json', interactive: true,
    render: (p, h) => <SigmaNetwork data={p as SigmaPayload} height="100%" {...h} />,
  },
  mermaid: {
    label: 'Mermaid', contentType: 'text', interactive: false,
    render: (p) => (
      <>
        <div className="panel-title">Rendered Mermaid (text input below)</div>
        <MermaidNetwork text={p as string} />
        <RawDetails label="raw mermaid" text={p as string} />
      </>
    ),
  },
  dot: {
    label: 'GraphViz DOT', contentType: 'text', interactive: false,
    render: (p) => (
      <>
        <div className="panel-title">Rendered GraphViz (DOT source below)</div>
        <DotNetwork text={p as string} />
        <RawDetails label="raw dot" text={p as string} />
      </>
    ),
  },
  svg: {
    label: 'SVG', contentType: 'text', interactive: false,
    render: (p) => <div className="svg-host" dangerouslySetInnerHTML={{ __html: p as string }} />,
  },
  png: {
    label: 'PNG (Skia)', contentType: 'binary-url', interactive: false,
    render: (p) => <div className="png-host"><img src={p as string} alt="rendered network" /></div>,
  },
  core: {
    label: 'Core JSON', contentType: 'json', interactive: false,
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
  const [theme, setTheme] = useState<ThemeKey>('light');
  const [picked, setPicked] = useState<SelectedItem | null>(null);
  const [hoveredId, setHoveredId] = useState<string | null>(null);
  const [editMode, setEditMode] = useState(false);

  const refreshSamples = useCallback(() => {
    fetch('/api/samples').then((r) => r.json()).then((data: SampleInfo[]) => {
      setSamples(data);
      if (data.length > 0 && !selected) setSelected(data[0]!.name);
    });
  }, [selected]);

  useEffect(() => { refreshSamples(); }, []);

  useEffect(() => { setPicked(null); setHoveredId(null); }, [selected, tab]);

  const handlers: NetworkEventHandlers = {
    onNodeClick: useCallback((id, data) => setPicked({ kind: 'node', id, data }), []),
    onEdgeClick: useCallback((id, data) => setPicked({ kind: 'edge', id, data }), []),
    onNodeHover: useCallback((id) => setHoveredId(id), []),
  };

  return (
    <div className={`layout theme-${theme}`}>
      <aside className="sidebar">
        <h1>DeepSigma<br/>Networks</h1>
        <div className="theme-toggle">
          <button className={theme === 'light' ? 'active' : ''} onClick={() => setTheme('light')}>Light</button>
          <button className={theme === 'dark' ? 'active' : ''} onClick={() => setTheme('dark')}>Dark</button>
        </div>
        <h2>Samples</h2>
        <ul className="sample-list">
          {samples.map((s) => (
            <li key={s.name} className={s.name === selected ? 'active' : ''} onClick={() => setSelected(s.name)}>
              <div>
                {s.title}
                {s.edited && <span className="edited-badge">edited</span>}
                {s.imported && <span className="imported-badge">imported</span>}
              </div>
              <div className="sample-meta">{s.nodeCount} nodes · {s.edgeCount} edges</div>
            </li>
          ))}
        </ul>
        <ImportPanel onImported={(id) => { refreshSamples(); setSelected(id); }} />
        <h2>Editor</h2>
        <button
          className={`edit-toggle ${editMode ? 'on' : ''}`}
          onClick={() => { setEditMode(!editMode); if (!editMode) setTab('reactflow'); }}
        >
          {editMode ? 'Exit edit mode' : 'Enter edit mode (ReactFlow)'}
        </button>
        <h2>Selection</h2>
        <SelectionPanel item={picked} interactive={TAB_CONFIG[tab].interactive} />
        {hoveredId && <div className="hover-hint">hovering: {hoveredId}</div>}
        <h2>About</h2>
        <p style={{ fontSize: 12, color: '#94a3b8', lineHeight: 1.5 }}>
          One Network model, rendered every way. Click any node in an interactive tab to see its payload.
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
            editMode && tab === 'reactflow' ? (
              <PanelErrorBoundary resetKey={`editor-${selected}-${theme}`}>
                <NetworkEditor key={`editor-${selected}-${theme}`} sample={selected} theme={theme} onSaved={refreshSamples} />
              </PanelErrorBoundary>
            ) : (
              <PanelErrorBoundary resetKey={`${selected}-${tab}-${theme}`}>
                <Panel key={`${selected}-${tab}-${theme}`} sample={selected} tab={tab} theme={theme} handlers={handlers} />
              </PanelErrorBoundary>
            )
          )}
        </div>
      </main>
    </div>
  );
}

function SelectionPanel({ item, interactive }: { item: SelectedItem | null; interactive: boolean }) {
  if (!interactive) return <div className="selection-empty">Switch to an interactive renderer (ReactFlow, Cytoscape, D3, Sigma) to click nodes.</div>;
  if (!item) return <div className="selection-empty">Click a node or edge.</div>;
  return (
    <div className="selection-card">
      <div className="selection-kind">{item.kind}</div>
      <div className="selection-id">{item.id}</div>
      {item.data && Object.keys(item.data).length > 0 && (
        <dl className="selection-data">
          {Object.entries(item.data).map(([k, v]) => (
            <div key={k} className="selection-row">
              <dt>{k}</dt>
              <dd>{String(v)}</dd>
            </div>
          ))}
        </dl>
      )}
    </div>
  );
}

function Panel({ sample, tab, theme, handlers }: { sample: string; tab: TabKey; theme: ThemeKey; handlers: NetworkEventHandlers }) {
  const [payload, setPayload] = useState<unknown>(null);
  const [loading, setLoading] = useState(true);
  const cfg = TAB_CONFIG[tab];

  useEffect(() => {
    setLoading(true);
    setPayload(null);
    const url = `/api/samples/${sample}/${tab}?theme=${theme}`;
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
  }, [sample, tab, theme, cfg.contentType]);

  if (loading || payload === null) return <div className="loading">Loading…</div>;
  return <>{cfg.render(payload, handlers)}</>;
}
