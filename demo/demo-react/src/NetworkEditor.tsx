import { useCallback, useEffect, useState } from 'react';
import ReactFlow, {
  Background,
  Controls,
  MiniMap,
  addEdge,
  applyEdgeChanges,
  applyNodeChanges,
  MarkerType,
  type Connection,
  type Edge,
  type EdgeChange,
  type Node,
  type NodeChange,
} from 'reactflow';
import 'reactflow/dist/style.css';
import type { ReactFlowPayload } from 'deepsigma-network-core';

export interface NetworkEditorProps {
  sample: string;
  theme: 'light' | 'dark';
  onSaved?: () => void;
}

export function NetworkEditor({ sample, theme, onSaved }: NetworkEditorProps) {
  const [nodes, setNodes] = useState<Node[]>([]);
  const [edges, setEdges] = useState<Edge[]>([]);
  const [bg, setBg] = useState<string>('#FFFFFF');
  const [busy, setBusy] = useState(false);
  const [status, setStatus] = useState<string>('');
  const [idCounter, setIdCounter] = useState(0);

  // Fetch ReactFlow payload as the initial editor state.
  useEffect(() => {
    setBusy(true);
    fetch(`/api/samples/${sample}/reactflow?theme=${theme}`)
      .then((r) => r.json())
      .then((p: ReactFlowPayload) => {
        setBg(p.theme.background);
        setNodes(p.nodes.map((n) => ({
          id: n.id,
          position: n.position,
          data: { label: n.data.label },
          type: n.parentNode ? 'default' : 'default',
          parentNode: n.parentNode,
          extent: n.parentNode ? 'parent' : undefined,
          style: {
            background: (n.style.backgroundColor as string) ?? '#fff',
            border: `${(n.style.borderWidth as number) ?? 1}px solid ${(n.style.borderColor as string) ?? '#999'}`,
            color: (n.style.color as string) ?? '#000',
            width: n.style.width as number | undefined,
            height: n.style.height as number | undefined,
            fontFamily: n.style.fontFamily as string | undefined,
            fontSize: n.style.fontSize as number | undefined,
          },
        })));
        setEdges(p.edges.map((e) => ({
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
        })));
        setBusy(false);
      })
      .catch(() => { setStatus('Load failed'); setBusy(false); });
  }, [sample, theme]);

  const onNodesChange = useCallback((changes: NodeChange[]) => setNodes((ns) => applyNodeChanges(changes, ns)), []);
  const onEdgesChange = useCallback((changes: EdgeChange[]) => setEdges((es) => applyEdgeChanges(changes, es)), []);
  const onConnect = useCallback((conn: Connection) => setEdges((es) => addEdge({
    ...conn,
    id: `e_new_${Date.now()}`,
    markerEnd: { type: MarkerType.ArrowClosed },
  }, es)), []);

  const addNode = () => {
    const id = `n_new_${idCounter}`;
    setIdCounter(idCounter + 1);
    setNodes((ns) => [...ns, {
      id,
      position: { x: 200 + Math.random() * 200, y: 200 + Math.random() * 200 },
      data: { label: `Node ${idCounter + 1}` },
      style: { background: '#10b981', color: '#fff', border: '1px solid #047857', padding: 8, borderRadius: 6 },
    }]);
  };

  const save = async () => {
    setBusy(true);
    setStatus('Saving…');
    // First fetch the canonical Core JSON to use as the structural base, then replace nodes and edges with edited ones.
    const baseEnv = await fetch(`/api/samples/${sample}/core`).then((r) => r.json());
    const baseNet = baseEnv.network;
    const edited = {
      ...baseNet,
      // Index original nodes for style preservation
      nodes: nodes.map((n) => {
        const original = baseNet.nodes.find((bn: { id: string }) => bn.id === n.id);
        return {
          ...(original ?? {}),
          id: n.id,
          label: (n.data?.label as string) ?? original?.label ?? n.id,
          position: { x: n.position.x, y: n.position.y },
          groupId: n.parentNode ?? null,
        };
      }),
      edges: edges.map((e) => {
        const original = baseNet.edges.find((be: { id: string }) => be.id === e.id);
        return {
          ...(original ?? {}),
          id: e.id,
          source: e.source,
          target: e.target,
          label: e.label ?? original?.label ?? null,
        };
      }),
    };
    const envelope = { format: baseEnv.format, version: baseEnv.version, network: edited };
    const resp = await fetch(`/api/edit/${sample}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(envelope),
    });
    if (resp.ok) { setStatus('Saved'); onSaved?.(); }
    else { const err = await resp.text(); setStatus(`Save failed: ${err.slice(0, 80)}`); }
    setBusy(false);
    setTimeout(() => setStatus(''), 1800);
  };

  const reset = async () => {
    setBusy(true);
    setStatus('Resetting…');
    await fetch(`/api/edit/${sample}`, { method: 'DELETE' });
    onSaved?.();
    // Re-fetch the now-pristine payload by reloading the effect.
    const p: ReactFlowPayload = await fetch(`/api/samples/${sample}/reactflow?theme=${theme}`).then((r) => r.json());
    setBg(p.theme.background);
    setNodes(p.nodes.map((n) => ({
      id: n.id,
      position: n.position,
      data: { label: n.data.label },
      parentNode: n.parentNode,
      extent: n.parentNode ? 'parent' : undefined,
      style: {
        background: (n.style.backgroundColor as string) ?? '#fff',
        border: `${(n.style.borderWidth as number) ?? 1}px solid ${(n.style.borderColor as string) ?? '#999'}`,
        color: (n.style.color as string) ?? '#000',
        width: n.style.width as number | undefined,
        height: n.style.height as number | undefined,
      },
    })));
    setEdges(p.edges.map((e) => ({
      id: e.id, source: e.source, target: e.target, label: e.label,
      markerEnd: e.markerEnd ? { type: MarkerType.ArrowClosed } : undefined,
    })));
    setStatus('Reset');
    setBusy(false);
    setTimeout(() => setStatus(''), 1800);
  };

  return (
    <div style={{ width: '100%', height: '100%', display: 'flex', flexDirection: 'column' }}>
      <div className="editor-toolbar">
        <button onClick={addNode} disabled={busy}>+ Add Node</button>
        <button onClick={save} disabled={busy} className="primary">Save Changes</button>
        <button onClick={reset} disabled={busy}>Reset to Original</button>
        <span className="editor-hint">Drag from a node's edge to connect · Select + Delete to remove</span>
        {status && <span className="editor-status">{status}</span>}
      </div>
      <div style={{ flex: 1, background: bg }}>
        <ReactFlow
          nodes={nodes}
          edges={edges}
          onNodesChange={onNodesChange}
          onEdgesChange={onEdgesChange}
          onConnect={onConnect}
          fitView
          deleteKeyCode={['Delete', 'Backspace']}
        >
          <Background />
          <Controls />
          <MiniMap />
        </ReactFlow>
      </div>
    </div>
  );
}
