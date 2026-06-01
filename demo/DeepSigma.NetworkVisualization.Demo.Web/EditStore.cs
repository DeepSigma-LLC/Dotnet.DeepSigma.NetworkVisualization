using System.Collections.Concurrent;
using DeepSigma.NetworkVisualization;

namespace DeepSigma.NetworkVisualization.Demo.Web;

public sealed class EditStore
{
    private readonly ConcurrentDictionary<string, Network> _edits = new();

    public bool TryGet(string sampleName, out Network network) => _edits.TryGetValue(sampleName, out network!);
    public void Set(string sampleName, Network network) => _edits[sampleName] = network;
    public bool Clear(string sampleName) => _edits.TryRemove(sampleName, out _);
    public bool HasEdit(string sampleName) => _edits.ContainsKey(sampleName);
}
