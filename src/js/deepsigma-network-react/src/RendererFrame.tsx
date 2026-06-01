import type { ReactNode } from 'react';

export type RendererState =
  | { kind: 'loading'; label?: string }
  | { kind: 'error'; message: string }
  | { kind: 'ready'; content: ReactNode };

export interface RendererFrameProps {
  state: RendererState;
  height?: number | string;
}

export function RendererFrame({ state, height = '100%' }: RendererFrameProps) {
  if (state.kind === 'loading') {
    return (
      <div style={{ width: '100%', height, display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#64748b', fontStyle: 'italic' }}>
        {state.label ?? 'Loading…'}
      </div>
    );
  }
  if (state.kind === 'error') {
    return (
      <div style={{ padding: 24, background: '#fef2f2', border: '1px solid #fecaca', borderRadius: 8, color: '#7f1d1d' }}>
        <strong>Renderer error</strong>
        <pre style={{ marginTop: 12, whiteSpace: 'pre-wrap', fontSize: 12 }}>{state.message}</pre>
      </div>
    );
  }
  return <>{state.content}</>;
}
