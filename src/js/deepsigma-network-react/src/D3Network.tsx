import { useEffect, useRef } from 'react';
import * as d3 from 'd3';
import type { D3Payload, NetworkEventHandlers } from 'deepsigma-network-core';

export interface D3NetworkProps extends NetworkEventHandlers {
  data: D3Payload;
  height?: number;
  width?: number;
}

interface SimNode extends d3.SimulationNodeDatum {
  id: string;
  label: string;
  fill: string;
  stroke: string;
  radius: number;
}

interface SimLink extends d3.SimulationLinkDatum<SimNode> {
  id: string;
  label?: string;
  stroke: string;
  strokeWidth: number;
  lineStyle: string;
}

export function D3Network({ data, height = 600, width = 800, onNodeClick, onEdgeClick, onNodeHover }: D3NetworkProps) {
  const svgRef = useRef<SVGSVGElement | null>(null);

  useEffect(() => {
    if (!svgRef.current) return;
    const dataById = new Map(data.nodes.map((n) => [n.id, (n as unknown as { data?: Record<string, unknown> }).data]));
    const svg = d3.select(svgRef.current);
    svg.selectAll('*').remove();
    svg.attr('viewBox', `0 0 ${width} ${height}`).style('background', data.theme.background);

    const nodes: SimNode[] = data.nodes.map((n) => ({
      id: n.id, label: n.label, fill: n.fill, stroke: n.stroke, radius: n.radius,
      fx: n.fx, fy: n.fy,
    }));
    const links: SimLink[] = data.links.map((l) => ({
      id: l.id, source: l.source, target: l.target, label: l.label,
      stroke: l.stroke, strokeWidth: l.strokeWidth, lineStyle: l.lineStyle,
    }));

    const sim = d3.forceSimulation(nodes)
      .force('link', d3.forceLink<SimNode, SimLink>(links).id((d) => d.id).distance(data.simulation.linkDistance))
      .force('charge', d3.forceManyBody().strength(data.simulation.charge))
      .force('center', d3.forceCenter(width / 2, height / 2))
      .alphaDecay(data.simulation.alphaDecay);

    if (data.directed) {
      svg.append('defs').append('marker')
        .attr('id', 'd3-arrow').attr('viewBox', '0 -5 10 10')
        .attr('refX', 18).attr('refY', 0)
        .attr('markerWidth', 6).attr('markerHeight', 6).attr('orient', 'auto')
        .append('path').attr('d', 'M0,-5L10,0L0,5').attr('fill', '#888');
    }

    const link = svg.append('g').selectAll('line').data(links).enter().append('line')
      .attr('stroke', (d) => d.stroke).attr('stroke-width', (d) => d.strokeWidth)
      .attr('stroke-dasharray', (d) => d.lineStyle === 'dashed' ? '5 4' : d.lineStyle === 'dotted' ? '1 3' : null)
      .attr('marker-end', data.directed ? 'url(#d3-arrow)' : null);
    if (onEdgeClick) {
      link.style('cursor', 'pointer').on('click', (_, d) => onEdgeClick(d.id, undefined));
    }

    const node = svg.append('g').selectAll<SVGCircleElement, SimNode>('circle').data(nodes).enter().append('circle')
      .attr('r', (d) => d.radius).attr('fill', (d) => d.fill).attr('stroke', (d) => d.stroke).attr('stroke-width', 1.5)
      .call(d3.drag<SVGCircleElement, SimNode>()
        .on('start', (event, d) => { if (!event.active) sim.alphaTarget(0.3).restart(); d.fx = d.x; d.fy = d.y; })
        .on('drag', (event, d) => { d.fx = event.x; d.fy = event.y; })
        .on('end', (event, d) => { if (!event.active) sim.alphaTarget(0); d.fx = null; d.fy = null; }));
    if (onNodeClick) {
      node.style('cursor', 'pointer').on('click', (_, d) => onNodeClick(d.id, dataById.get(d.id)));
    }
    node.on('mouseenter', (_, d) => onNodeHover?.(d.id))
        .on('mouseleave', () => onNodeHover?.(null));

    const label = svg.append('g').selectAll('text').data(nodes).enter().append('text')
      .text((d) => d.label).attr('font-size', data.theme.fontSize)
      .attr('font-family', data.theme.fontFamily).attr('fill', data.theme.labelColor)
      .attr('text-anchor', 'middle').attr('dy', (d) => d.radius + 12);

    sim.on('tick', () => {
      link
        .attr('x1', (d) => (d.source as SimNode).x!)
        .attr('y1', (d) => (d.source as SimNode).y!)
        .attr('x2', (d) => (d.target as SimNode).x!)
        .attr('y2', (d) => (d.target as SimNode).y!);
      node.attr('cx', (d) => d.x!).attr('cy', (d) => d.y!);
      label.attr('x', (d) => d.x!).attr('y', (d) => d.y!);
    });

    return () => { sim.stop(); };
  }, [data, height, width]);

  return <svg ref={svgRef} width="100%" height={height} />;
}
