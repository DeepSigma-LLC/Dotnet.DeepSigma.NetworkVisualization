import { useEffect, useState } from 'react';
import mermaid from 'mermaid';
import { RendererFrame, type RendererState } from './RendererFrame';

let inited = false;

export interface MermaidNetworkProps {
  text: string;
  theme?: 'default' | 'dark' | 'forest' | 'neutral';
}

export function MermaidNetwork({ text, theme = 'default' }: MermaidNetworkProps) {
  const [state, setState] = useState<RendererState>({ kind: 'loading', label: 'Rendering Mermaid…' });

  useEffect(() => {
    if (!inited) {
      mermaid.initialize({ startOnLoad: false, theme, securityLevel: 'loose' });
      inited = true;
    }
    let cancelled = false;
    setState({ kind: 'loading', label: 'Rendering Mermaid…' });
    const id = `mmd-${Math.random().toString(36).slice(2)}`;
    mermaid.render(id, text)
      .then((result) => {
        if (cancelled) return;
        setState({ kind: 'ready', content: <div dangerouslySetInnerHTML={{ __html: result.svg }} /> });
      })
      .catch((e: Error) => {
        if (!cancelled) setState({ kind: 'error', message: e.message });
      });
    return () => { cancelled = true; };
  }, [text, theme]);

  return <RendererFrame state={state} />;
}
