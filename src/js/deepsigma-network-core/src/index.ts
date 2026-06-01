export type NodeShape =
  | 'Ellipse' | 'Circle' | 'Rectangle' | 'RoundedRectangle' | 'Diamond'
  | 'Hexagon' | 'Triangle' | 'Parallelogram' | 'Cylinder' | 'Custom';

export type LineStyle = 'Solid' | 'Dashed' | 'Dotted';

export type ArrowStyle = 'None' | 'Triangle' | 'Open' | 'Diamond' | 'Circle' | 'Vee';

export type LayoutAlgorithm =
  | 'None' | 'Grid' | 'Circular' | 'Tree' | 'ForceDirected'
  | 'Hierarchical' | 'Sugiyama' | 'Radial' | 'Mds';

export type LayoutDirection = 'TopToBottom' | 'BottomToTop' | 'LeftToRight' | 'RightToLeft';

export interface Position { x: number; y: number; }
export type HexColor = string;

export interface NodeStyle {
  fill?: HexColor;
  stroke?: HexColor;
  strokeWidth?: number;
  shape?: NodeShape;
  labelColor?: HexColor;
  fontFamily?: string;
  fontSize?: number;
  width?: number;
  height?: number;
  icon?: string;
  cssClass?: string;
  customAttributes?: Record<string, string>;
}

export interface EdgeAppearance {
  stroke?: HexColor;
  strokeWidth?: number;
  lineStyle?: LineStyle;
  sourceArrow?: ArrowStyle;
  targetArrow?: ArrowStyle;
  labelColor?: HexColor;
  fontFamily?: string;
  fontSize?: number;
  curvature?: number;
  cssClass?: string;
}

export interface Theme {
  background: HexColor;
  defaultNodeFill: HexColor;
  defaultNodeStroke: HexColor;
  defaultEdgeStroke: HexColor;
  defaultLabelColor: HexColor;
  defaultFontFamily: string;
  defaultFontSize: number;
}

export interface Node {
  id: string;
  label?: string;
  style?: NodeStyle;
  position?: Position;
  groupId?: string;
  tooltip?: string;
  url?: string;
  data?: Record<string, unknown>;
}

export interface Edge {
  id: string;
  source: string;
  target: string;
  label?: string;
  weight?: number;
  style?: EdgeAppearance;
  data?: Record<string, unknown>;
}

export interface Group {
  id: string;
  label?: string;
  style?: NodeStyle;
  parentGroupId?: string;
  collapsed?: boolean;
  memberNodeIds?: string[];
}

export interface LayoutSettings {
  algorithm: LayoutAlgorithm;
  direction: LayoutDirection;
  nodeSpacing: number;
  rankSpacing: number;
  padding: number;
  randomSeed?: number;
  options?: Record<string, unknown>;
}

export interface InteractionSettings {
  zoomEnabled: boolean;
  panEnabled: boolean;
  nodeDragEnabled: boolean;
  selectionEnabled: boolean;
  hoverHighlightEnabled: boolean;
  fitOnLoad: boolean;
  minZoom: number;
  maxZoom: number;
}

export interface Network {
  directed: boolean;
  title?: string;
  nodes: Node[];
  edges: Edge[];
  groups: Group[];
  layout: LayoutSettings;
  interaction: InteractionSettings;
  theme: Theme;
  metadata?: Record<string, unknown>;
}

export interface NetworkEnvelope {
  format: 'deepsigma.network';
  version: string;
  network: Network;
}

export interface ReactFlowPayload {
  format: 'reactflow';
  version: string;
  directed: boolean;
  theme: Theme;
  interaction: InteractionSettings;
  nodes: Array<{
    id: string;
    position: Position;
    data: { label: string; tooltip?: string; custom?: Record<string, unknown> };
    type: string;
    parentNode?: string;
    style: Record<string, unknown>;
  }>;
  edges: Array<{
    id: string;
    source: string;
    target: string;
    label?: string;
    type: string;
    animated: boolean;
    markerEnd?: { type: string } | null;
    style: Record<string, unknown>;
    data?: Record<string, unknown>;
  }>;
  groups: Array<{ id: string; label?: string; parent?: string }>;
}

export interface CytoscapePayload {
  format: 'cytoscape';
  version: string;
  directed: boolean;
  elements: {
    nodes: Array<Record<string, unknown>>;
    edges: Array<Record<string, unknown>>;
  };
  style: Array<Record<string, unknown>>;
  layout: Record<string, unknown>;
  interaction: InteractionSettings;
}

export interface D3Payload {
  format: 'd3-force';
  version: string;
  directed: boolean;
  theme: { background: HexColor; fontFamily: string; fontSize: number; labelColor: HexColor };
  simulation: { charge: number; linkDistance: number; alpha: number; alphaDecay: number };
  interaction: InteractionSettings;
  nodes: Array<{
    id: string;
    label: string;
    group?: string;
    tooltip?: string;
    fx?: number;
    fy?: number;
    shape: string;
    fill: HexColor;
    stroke: HexColor;
    radius: number;
  }>;
  links: Array<{
    id: string;
    source: string;
    target: string;
    label?: string;
    value: number;
    stroke: HexColor;
    strokeWidth: number;
    lineStyle: string;
  }>;
  groups: Array<{ id: string; label?: string; parent?: string }>;
}

export interface NetworkEventHandlers {
  onNodeClick?: (id: string, data?: Record<string, unknown>) => void;
  onEdgeClick?: (id: string, data?: Record<string, unknown>) => void;
  onNodeHover?: (id: string | null) => void;
}

export interface SigmaPayload {
  format: 'sigma';
  version: string;
  theme: { background: HexColor; fontFamily: string; fontSize: number; labelColor: HexColor };
  interaction: InteractionSettings;
  graph: {
    attributes: Record<string, unknown>;
    options: { type: 'directed' | 'undirected'; multi: boolean; allowSelfLoops: boolean };
    nodes: Array<{ key: string; attributes: Record<string, unknown> }>;
    edges: Array<{ key: string; source: string; target: string; attributes: Record<string, unknown> }>;
  };
}

export function parseHexColor(hex: HexColor): { r: number; g: number; b: number; a: number } {
  const s = hex.startsWith('#') ? hex.slice(1) : hex;
  const dup = (c: string) => parseInt(c + c, 16);
  if (s.length === 3) return { r: dup(s[0]!), g: dup(s[1]!), b: dup(s[2]!), a: 255 };
  if (s.length === 4) return { r: dup(s[0]!), g: dup(s[1]!), b: dup(s[2]!), a: dup(s[3]!) };
  if (s.length === 6) return {
    r: parseInt(s.slice(0, 2), 16), g: parseInt(s.slice(2, 4), 16),
    b: parseInt(s.slice(4, 6), 16), a: 255,
  };
  if (s.length === 8) return {
    r: parseInt(s.slice(0, 2), 16), g: parseInt(s.slice(2, 4), 16),
    b: parseInt(s.slice(4, 6), 16), a: parseInt(s.slice(6, 8), 16),
  };
  throw new Error(`Invalid hex color '${hex}'`);
}
