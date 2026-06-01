import { useEffect, useRef, useState } from 'react';
import { Graphviz } from '@hpcc-js/wasm-graphviz';

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
  const containerRef = useRef<HTMLDivElement | null>(null);
  const [svg, setSvg] = useState<string>('');
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setErr(null);
    loadGraphviz()
      .then((gv) => {
        if (cancelled) return;
        try {
          const out = gv.layout(text, 'svg', engine);
          setSvg(out);
        } catch (e) {
          setErr(e instanceof Error ? e.message : String(e));
        }
      })
      .catch((e: Error) => {
        if (!cancelled) setErr(e.message);
      });
    return () => { cancelled = true; };
  }, [text, engine]);

  if (err) return <pre style={{ color: 'crimson', whiteSpace: 'pre-wrap' }}>{err}</pre>;
  if (!svg) return <div style={{ color: '#64748b', fontStyle: 'italic' }}>Loading GraphViz…</div>;
  return <div ref={containerRef} dangerouslySetInnerHTML={{ __html: svg }} />;
}
