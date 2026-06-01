import { useEffect, useRef, useState } from 'react';
import mermaid from 'mermaid';

let inited = false;

export interface MermaidNetworkProps {
  text: string;
  theme?: 'default' | 'dark' | 'forest' | 'neutral';
}

export function MermaidNetwork({ text, theme = 'default' }: MermaidNetworkProps) {
  const ref = useRef<HTMLDivElement | null>(null);
  const [svg, setSvg] = useState<string>('');
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    if (!inited) {
      mermaid.initialize({ startOnLoad: false, theme, securityLevel: 'loose' });
      inited = true;
    }
    let cancelled = false;
    const id = `mmd-${Math.random().toString(36).slice(2)}`;
    mermaid.render(id, text)
      .then((result) => { if (!cancelled) { setSvg(result.svg); setErr(null); } })
      .catch((e: Error) => { if (!cancelled) { setErr(e.message); } });
    return () => { cancelled = true; };
  }, [text, theme]);

  if (err) return <pre style={{ color: 'crimson' }}>{err}</pre>;
  return <div ref={ref} dangerouslySetInnerHTML={{ __html: svg }} />;
}
