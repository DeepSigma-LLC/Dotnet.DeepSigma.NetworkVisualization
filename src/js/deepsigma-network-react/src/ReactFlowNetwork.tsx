import { useMemo } from 'react';
import ReactFlow, { Background, Controls, MiniMap } from 'reactflow';
import 'reactflow/dist/style.css';
import type { ReactFlowPayload, NetworkEventHandlers } from 'deepsigma-network-core';
import { payloadToReactFlowNodes, payloadToReactFlowEdges } from './reactflowMapping';

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
  const nodes = useMemo(() => payloadToReactFlowNodes(data), [data]);
  const edges = useMemo(() => payloadToReactFlowEdges(data), [data]);

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
