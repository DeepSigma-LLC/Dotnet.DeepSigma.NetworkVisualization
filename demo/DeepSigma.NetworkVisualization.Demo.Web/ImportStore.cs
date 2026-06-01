using System.Collections.Concurrent;
using DeepSigma.NetworkVisualization;

namespace DeepSigma.NetworkVisualization.Demo.Web;

public sealed class ImportStore
{
    private readonly ConcurrentDictionary<string, Network> _imports = new();
    private int _counter;

    public string Add(Network network)
    {
        var id = $"import-{Interlocked.Increment(ref _counter)}";
        _imports[id] = network;
        return id;
    }

    public bool TryGet(string id, out Network network) => _imports.TryGetValue(id, out network!);
    public IReadOnlyDictionary<string, Network> All => _imports;
    public bool Remove(string id) => _imports.TryRemove(id, out _);
}
