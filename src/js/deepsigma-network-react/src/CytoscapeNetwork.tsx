import { useEffect, useRef } from 'react';
import cytoscape, { type Core } from 'cytoscape';
// @ts-expect-error - cytoscape-dagre has no first-class types
import dagre from 'cytoscape-dagre';
import type { CytoscapePayload } from 'deepsigma-network-core';

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

export interface CytoscapeNetworkProps {
  data: CytoscapePayload;
  height?: number | string;
}

const BUILT_IN_LAYOUTS = new Set([
  'null', 'random', 'preset', 'grid', 'circle', 'concentric', 'breadthfirst', 'cose',
]);

export function CytoscapeNetwork({ data, height = 600 }: CytoscapeNetworkProps) {
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

    cyRef.current = cytoscape({
      container: containerRef.current,
      elements: [
        ...data.elements.nodes.map((n) => ({ group: 'nodes', ...n })),
        ...data.elements.edges.map((e) => ({ group: 'edges', ...e })),
      ] as unknown as cytoscape.ElementDefinition[],
      style: data.style as unknown as cytoscape.StylesheetStyle[],
      layout: layout as unknown as cytoscape.LayoutOptions,
      wheelSensitivity: 0.2,
    });
    return () => { cyRef.current?.destroy(); cyRef.current = null; };
  }, [data]);

  return <div ref={containerRef} style={{ width: '100%', height }} />;
}
