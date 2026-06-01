import { useEffect, useState } from 'react';
import { Graphviz } from '@hpcc-js/wasm-graphviz';
import { RendererFrame, type RendererState } from './RendererFrame';

let graphvizPromise: Promise<Graphviz> | null = null;
function loadGraphviz() {
  if (!graphvizPromise) graphvizPromise = Graphviz.load();
  return graphvizPromise;
}

export interface DotNetworkProps {
  text: string;
  engine?: 'dot' | 'neato' | 'fdp' | 'sfdp' | 'twopi' | 'circo';
}

export function DotNetwork({ text, engine = 'dot' }: DotNetworkProps) {
  const [state, setState] = useState<RendererState>({ kind: 'loading', label: 'Loading GraphViz…' });

  useEffect(() => {
    let cancelled = false;
    setState({ kind: 'loading', label: 'Loading GraphViz…' });
    loadGraphviz()
      .then((gv) => {
        if (cancelled) return;
        try {
          const svg = gv.layout(text, 'svg', engine);
          setState({ kind: 'ready', content: <div dangerouslySetInnerHTML={{ __html: svg }} /> });
        } catch (e) {
          setState({ kind: 'error', message: e instanceof Error ? e.message : String(e) });
        }
      })
      .catch((e: Error) => {
        if (!cancelled) setState({ kind: 'error', message: e.message });
      });
    return () => { cancelled = true; };
  }, [text, engine]);

  return <RendererFrame state={state} />;
}
