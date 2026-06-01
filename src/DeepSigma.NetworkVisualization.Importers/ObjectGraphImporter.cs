using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using DeepSigma.NetworkVisualization.Builders;

namespace DeepSigma.NetworkVisualization.Importers;

/// <summary>
/// Walks a runtime .NET object's public instance properties and produces a <see cref="Network"/> with
/// one node per object, one node per primitive value, and one group per collection. Cycles are detected by
/// reference identity and rendered as edges back to the previously visited node.
/// </summary>
internal static class ObjectGraphImporter
{
    public static Network Import(object root, ObjectGraphOptions options)
    {
        ArgumentNullException.ThrowIfNull(root);

        var nb = NetworkBuilder.Create()
            .Directed()
            .Title(options.RootLabel ?? root.GetType().Name)
            .WithLayout(l => l.Algorithm(LayoutAlgorithm.Hierarchical).Direction(LayoutDirection.TopToBottom).NodeSpacing(40).RankSpacing(70));

        var visited = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);
        var idCounter = 0;

        // Root node
        var rootLabel = options.RootLabel ?? root.GetType().Name;
        var rootId = NewId("root", ref idCounter);
        nb.AddNode(rootId, n => n
            .Label(rootLabel)
            .Shape(NodeShape.RoundedRectangle)
            .Fill("#1976D2").LabelColor("#FFFFFF")
            .Size(140, 50)
            .Tooltip(root.GetType().FullName ?? root.GetType().Name)
            .Data("type", root.GetType().FullName ?? root.GetType().Name));
        visited[root] = rootId;

        Walk(nb, root, rootId, depth: 0, ref idCounter, visited, options);
        return nb.Build();
    }

    private static void Walk(
        NetworkBuilder nb,
        object current,
        string parentId,
        int depth,
        ref int idCounter,
        Dictionary<object, string> visited,
        ObjectGraphOptions options)
    {
        if (depth >= options.MaxDepth)
        {
            var ellipsisId = NewId("ellipsis", ref idCounter);
            nb.AddNode(ellipsisId, n => n.Label("…").Shape(NodeShape.Ellipse).Fill("#94A3B8").Size(50, 30));
            nb.AddEdge(parentId, ellipsisId);
            return;
        }

        var type = current.GetType();
        var properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Concat(type.BaseType is { } b && b != typeof(object)
                ? b.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                : Array.Empty<PropertyInfo>())
            .Where(p => p.GetIndexParameters().Length == 0 && p.CanRead);

        foreach (var prop in properties)
        {
            if (options.PropertyFilter is { } pf && !pf(prop)) continue;

            object? value;
            try { value = prop.GetValue(current); }
            catch { continue; } // skip properties that throw on access

            if (value is null)
            {
                if (!options.IncludeNullValues) continue;
                var nullId = NewId(prop.Name, ref idCounter);
                nb.AddNode(nullId, n => n.Label($"{prop.Name} = null").Shape(NodeShape.Ellipse).Fill("#E2E8F0").LabelColor("#475569"));
                nb.AddEdge(parentId, nullId);
                continue;
            }

            var valueType = value.GetType();

            // Case 1: collection → group with one child per item.
            // Check this BEFORE leaf, because List<T>/T[]/Dictionary<,> live in System.* and the default leaf rule would otherwise swallow them.
            if (value is IEnumerable enumerable && value is not string)
            {
                var groupId = NewId(prop.Name + "_group", ref idCounter);
                var itemElementType = GetEnumerableElementType(valueType);
                nb.AddNode(groupId, n => n
                    .Label($"{prop.Name} ({itemElementType?.Name ?? "items"})")
                    .Shape(NodeShape.Hexagon)
                    .Fill("#FFCA28").LabelColor("#7C4A03")
                    .Size(140, 50)
                    .Data("property", prop.Name).Data("kind", "collection"));
                nb.AddEdge(parentId, groupId);

                var count = 0;
                foreach (var item in enumerable)
                {
                    if (count >= options.MaxCollectionItems)
                    {
                        var truncId = NewId(prop.Name + "_more", ref idCounter);
                        nb.AddNode(truncId, n => n.Label($"… +more").Shape(NodeShape.Ellipse).Fill("#94A3B8"));
                        nb.AddEdge(groupId, truncId);
                        break;
                    }
                    EmitChild(nb, item, groupId, prop.Name, $"[{count}]", depth + 1, ref idCounter, visited, options);
                    count++;
                }
                continue;
            }

            // Case 2: leaf-typed value → one labeled value node
            if (options.IsLeaf(valueType))
            {
                var rendered = RenderValue(value, options);
                var leafId = NewId(prop.Name, ref idCounter);
                nb.AddNode(leafId, n => n
                    .Label($"{prop.Name} = {rendered}")
                    .Shape(NodeShape.Rectangle)
                    .Fill("#F1F5F9").Stroke("#94A3B8").LabelColor("#1E293B")
                    .Tooltip(valueType.Name)
                    .Data("property", prop.Name).Data("type", valueType.Name).Data("value", value.ToString() ?? ""));
                nb.AddEdge(parentId, leafId);
                continue;
            }

            // Case 3: complex object → walk it (with cycle detection)
            EmitChild(nb, value, parentId, prop.Name, prop.Name, depth + 1, ref idCounter, visited, options);
        }
    }

    private static void EmitChild(
        NetworkBuilder nb,
        object? value,
        string parentId,
        string propertyName,
        string displayLabel,
        int depth,
        ref int idCounter,
        Dictionary<object, string> visited,
        ObjectGraphOptions options)
    {
        if (value is null) return;
        var valueType = value.GetType();

        if (options.IsLeaf(valueType))
        {
            var rendered = RenderValue(value, options);
            var leafId = NewId(propertyName + "_item", ref idCounter);
            nb.AddNode(leafId, n => n
                .Label($"{displayLabel} = {rendered}")
                .Shape(NodeShape.Rectangle)
                .Fill("#F1F5F9").Stroke("#94A3B8").LabelColor("#1E293B"));
            nb.AddEdge(parentId, leafId);
            return;
        }

        // Cycle: we've already emitted a node for this exact reference. Connect to it instead.
        if (visited.TryGetValue(value, out var existingId))
        {
            nb.AddEdge(parentId, existingId, e => e.Label("ref").Dashed());
            return;
        }

        var childId = NewId(propertyName + "_child", ref idCounter);
        nb.AddNode(childId, n => n
            .Label($"{displayLabel} : {valueType.Name}")
            .Shape(NodeShape.RoundedRectangle)
            .Fill("#42A5F5").LabelColor("#FFFFFF")
            .Size(140, 45)
            .Tooltip(valueType.FullName ?? valueType.Name)
            .Data("property", propertyName).Data("type", valueType.FullName ?? valueType.Name));
        nb.AddEdge(parentId, childId);
        visited[value] = childId;

        Walk(nb, value, childId, depth, ref idCounter, visited, options);
    }

    private static string RenderValue(object value, ObjectGraphOptions options)
    {
        var s = value switch
        {
            string str => $"\"{str}\"",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
        if (s.Length > options.MaxStringLength)
            s = string.Concat(s.AsSpan(0, options.MaxStringLength), "…");
        return s;
    }

    private static Type? GetEnumerableElementType(Type collectionType)
    {
        if (collectionType.IsArray) return collectionType.GetElementType();
        var ienum = collectionType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return ienum?.GetGenericArguments()[0];
    }

    private static string NewId(string hint, ref int counter)
    {
        counter++;
        // Sanitize: keep alnum + underscore, prefix with counter so ids are unique even if hints collide.
        var clean = new string(hint.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray());
        return $"n{counter}_{clean}";
    }
}
