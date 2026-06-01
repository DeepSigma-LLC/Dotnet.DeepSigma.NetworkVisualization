import { useEffect, useRef, useState } from 'react';
import Graph from 'graphology';
import Sigma from 'sigma';
import type { SigmaPayload, NetworkEventHandlers } from 'deepsigma-network-core';
import { RendererFrame, type RendererState } from './RendererFrame';

export interface SigmaNetworkProps extends NetworkEventHandlers {
  data: SigmaPayload;
  height?: number | string;
}

export function SigmaNetwork({ data, height = 600, onNodeClick, onEdgeClick, onNodeHover }: SigmaNetworkProps) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const sigmaRef = useRef<Sigma | null>(null);
  const [state, setState] = useState<RendererState>({ kind: 'loading', label: 'Mounting Sigma…' });

  useEffect(() => {
    if (!containerRef.current) return;
    try {
      if (sigmaRef.current) { sigmaRef.current.kill(); sigmaRef.current = null; }
      const graph = new Graph({
        type: data.graph.options.type,
        multi: data.graph.options.multi,
        allowSelfLoops: data.graph.options.allowSelfLoops,
      });
      for (const node of data.graph.nodes) {
        graph.addNode(node.key, node.attributes);
      }
      for (const edge of data.graph.edges) {
        graph.addEdgeWithKey(edge.key, edge.source, edge.target, edge.attributes);
      }
      const sigma = new Sigma(graph, containerRef.current, {
        labelFont: data.theme.fontFamily,
        labelSize: data.theme.fontSize,
        labelColor: { color: data.theme.labelColor },
        renderEdgeLabels: true,
        defaultEdgeColor: data.theme.labelColor,
      });
      sigma.on('clickNode', ({ node }) => {
        const attrs = graph.getNodeAttributes(node) as Record<string, unknown>;
        onNodeClick?.(node, attrs);
      });
      sigma.on('clickEdge', ({ edge }) => {
        const attrs = graph.getEdgeAttributes(edge) as Record<string, unknown>;
        onEdgeClick?.(edge, attrs);
      });
      sigma.on('enterNode', ({ node }) => onNodeHover?.(node));
      sigma.on('leaveNode', () => onNodeHover?.(null));
      sigmaRef.current = sigma;
      setState({ kind: 'ready', content: null });
    } catch (e) {
      setState({ kind: 'error', message: e instanceof Error ? e.message : String(e) });
    }
    return () => { sigmaRef.current?.kill(); sigmaRef.current = null; };
  }, [data]);

  if (state.kind === 'error') return <RendererFrame state={state} />;
  return (
    <div style={{ width: '100%', height, position: 'relative', background: data.theme.background }}>
      <div ref={containerRef} style={{ width: '100%', height: '100%' }} />
    </div>
  );
}
