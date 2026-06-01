import { useMemo } from 'react';
import ReactFlow, {
  Background,
  Controls,
  MiniMap,
  type Node as RfNode,
  type Edge as RfEdge,
  MarkerType,
} from 'reactflow';
import 'reactflow/dist/style.css';
import type { ReactFlowPayload, NetworkEventHandlers } from 'deepsigma-network-core';

export interface ReactFlowNetworkProps extends NetworkEventHandlers {
  data: ReactFlowPayload;
  height?: number | string;
  showMiniMap?: boolean;
  showControls?: boolean;
}

export function ReactFlowNetwork({
  data,
  height = 600,
  showMiniMap = true,
  showControls = true,
  onNodeClick,
  onEdgeClick,
  onNodeHover,
}: ReactFlowNetworkProps) {
  const nodesById = useMemo(() => new Map(data.nodes.map((n) => [n.id, n])), [data]);
  const edgesById = useMemo(() => new Map(data.edges.map((e) => [e.id, e])), [data]);

  const nodes = useMemo<RfNode[]>(() => data.nodes.map((n) => ({
    id: n.id,
    position: n.position,
    data: { label: n.data.label },
    type: 'default',
    style: {
      background: (n.style.backgroundColor as string) ?? '#fff',
      border: `${(n.style.borderWidth as number) ?? 1}px solid ${(n.style.borderColor as string) ?? '#999'}`,
      color: (n.style.color as string) ?? '#000',
      borderRadius: n.type === 'rounded' ? 12 : n.type === 'circle' || n.type === 'ellipse' ? '50%' : 4,
      width: n.style.width as number | undefined,
      height: n.style.height as number | undefined,
      fontFamily: n.style.fontFamily as string | undefined,
      fontSize: n.style.fontSize as number | undefined,
    },
    parentNode: n.parentNode,
  })), [data]);

  const edges = useMemo<RfEdge[]>(() => data.edges.map((e) => ({
    id: e.id,
    source: e.source,
    target: e.target,
    label: e.label,
    type: e.type === 'smoothstep' ? 'smoothstep' : 'default',
    markerEnd: e.markerEnd ? { type: MarkerType.ArrowClosed } : undefined,
    style: {
      stroke: (e.style.stroke as string) ?? '#888',
      strokeWidth: (e.style.strokeWidth as number) ?? 1,
      strokeDasharray: (e.style.strokeDasharray as string) ?? undefined,
    },
  })), [data]);

  return (
    <div style={{ width: '100%', height, background: data.theme.background }}>
      <ReactFlow
        nodes={nodes}
        edges={edges}
        fitView
        attributionPosition="bottom-right"
        onNodeClick={(_, n) => {
          const src = nodesById.get(n.id);
          onNodeClick?.(n.id, src?.data.custom);
        }}
        onEdgeClick={(_, e) => {
          const src = edgesById.get(e.id);
          onEdgeClick?.(e.id, src?.data);
        }}
        onNodeMouseEnter={(_, n) => onNodeHover?.(n.id)}
        onNodeMouseLeave={() => onNodeHover?.(null)}
      >
        {showMiniMap && <MiniMap />}
        {showControls && <Controls />}
        <Background />
      </ReactFlow>
    </div>
  );
}
