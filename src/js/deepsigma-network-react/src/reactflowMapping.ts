import {
  MarkerType,
  type Edge as RfEdge,
  type Node as RfNode,
} from 'reactflow';
import type { ReactFlowPayload } from 'deepsigma-network-core';

/**
 * Map the canonical ReactFlow payload (as emitted by the .NET ReactFlowRenderer)
 * into the array shapes ReactFlow's React component expects. Used by both
 * <ReactFlowNetwork> (read-only view) and the demo's <NetworkEditor> (editable copy).
 */
export function payloadToReactFlowNodes(payload: ReactFlowPayload): RfNode[] {
  return payload.nodes.map((n) => ({
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
    extent: n.parentNode ? ('parent' as const) : undefined,
  }));
}

export function payloadToReactFlowEdges(payload: ReactFlowPayload): RfEdge[] {
  return payload.edges.map((e) => ({
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
  }));
}
