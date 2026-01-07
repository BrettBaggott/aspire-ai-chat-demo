using System.Collections.Concurrent;

public class InMemoryCancellationManager : ICancellationManager
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _tokens = new();

    public CancellationToken GetCancellationToken(Guid id)
    {
        var cts = new CancellationTokenSource();
        _tokens[id] = cts;
        return cts.Token;
    }

    public Task CancelAsync(Guid id)
    {
        if (_tokens.TryRemove(id, out var cts))
        {
            cts.Cancel();
        }

        return Task.CompletedTask;
    }
}
