import { useEffect, useRef } from 'react';
import cytoscape, { type Core } from 'cytoscape';
// @ts-expect-error - cytoscape-dagre has no first-class types
import dagre from 'cytoscape-dagre';
import type { CytoscapePayload, NetworkEventHandlers } from 'deepsigma-network-core';

let dagreRegistered = false;
function ensureDagre() {
  if (dagreRegistered) return;
  try {
    cytoscape.use(dagre);
    dagreRegistered = true;
  } catch {
    // extension already registered or unavailable; fall back at layout time
  }
}

export interface CytoscapeNetworkProps extends NetworkEventHandlers {
  data: CytoscapePayload;
  height?: number | string;
}

const BUILT_IN_LAYOUTS = new Set([
  'null', 'random', 'preset', 'grid', 'circle', 'concentric', 'breadthfirst', 'cose',
]);

export function CytoscapeNetwork({ data, height = 600, onNodeClick, onEdgeClick, onNodeHover }: CytoscapeNetworkProps) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const cyRef = useRef<Core | null>(null);

  useEffect(() => {
    ensureDagre();
    if (!containerRef.current) return;
    if (cyRef.current) { cyRef.current.destroy(); cyRef.current = null; }

    const requestedLayout = (data.layout as { name?: string })?.name ?? 'cose';
    const layoutName = (requestedLayout === 'dagre' && dagreRegistered)
      ? 'dagre'
      : (BUILT_IN_LAYOUTS.has(requestedLayout) ? requestedLayout : 'cose');
    const layout = { ...(data.layout as object), name: layoutName };

    const cy = cytoscape({
      container: containerRef.current,
      elements: [
        ...data.elements.nodes.map((n) => ({ group: 'nodes', ...n })),
        ...data.elements.edges.map((e) => ({ group: 'edges', ...e })),
      ] as unknown as cytoscape.ElementDefinition[],
      style: data.style as unknown as cytoscape.StylesheetStyle[],
      layout: layout as unknown as cytoscape.LayoutOptions,
      wheelSensitivity: 0.2,
    });
    cy.on('tap', 'node', (evt) => {
      const ele = evt.target;
      onNodeClick?.(ele.id() as string, ele.data('custom') as Record<string, unknown> | undefined);
    });
    cy.on('tap', 'edge', (evt) => {
      const ele = evt.target;
      onEdgeClick?.(ele.id() as string, ele.data() as Record<string, unknown>);
    });
    cy.on('mouseover', 'node', (evt) => onNodeHover?.(evt.target.id() as string));
    cy.on('mouseout', 'node', () => onNodeHover?.(null));
    cyRef.current = cy;
    return () => { cyRef.current?.destroy(); cyRef.current = null; };
    // Intentionally omit callbacks from deps so re-renders don't destroy and rebuild the graph.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [data]);

  return <div ref={containerRef} style={{ width: '100%', height }} />;
}
