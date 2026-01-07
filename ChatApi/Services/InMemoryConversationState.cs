using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

public class InMemoryConversationState : IConversationState
{
    private static readonly ConcurrentDictionary<Guid, List<Action<ClientMessageFragment>>> Subscribers = new();
    private readonly ConcurrentDictionary<Guid, List<ClientMessageFragment>> _backlog = new();

    public Task PublishFragmentAsync(Guid conversationId, ClientMessageFragment fragment)
    {
        var backlog = _backlog.GetOrAdd(conversationId, _ => []);
        lock (backlog)
        {
            backlog.Add(fragment);
        }

        if (Subscribers.TryGetValue(conversationId, out var subscribers))
        {
            lock (subscribers)
            {
                foreach (var subscriber in subscribers)
                {
                    subscriber(fragment);
                }
            }
        }

        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<ClientMessageFragment> Subscribe(
        Guid conversationId,
        Guid? lastMessageId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_backlog.TryGetValue(conversationId, out var backlog))
        {
            List<ClientMessageFragment> snapshot;
            lock (backlog)
            {
                snapshot = backlog.ToList();
            }

            foreach (var fragment in snapshot)
            {
                if (lastMessageId is null || fragment.Id > lastMessageId)
                {
                    yield return fragment;
                }
            }
        }

        var channel = Channel.CreateUnbounded<ClientMessageFragment>();

        void LocalCallback(ClientMessageFragment fragment)
        {
            if (lastMessageId is null || fragment.Id > lastMessageId)
            {
                channel.Writer.TryWrite(fragment);
            }
        }

        AddLocalSubscriber(conversationId, LocalCallback);

        try
        {
            using var reg = cancellationToken.Register(() => channel.Writer.TryComplete());
            await foreach (var fragment in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return fragment;
            }
        }
        finally
        {
            RemoveLocalSubscriber(conversationId, LocalCallback);
        }
    }

    public Task CompleteAsync(Guid conversationId, Guid messageId) => Task.CompletedTask;

    public Task<IList<ClientMessage>> GetUnpublishedMessagesAsync(Guid conversationId)
    {
        var messages = new List<ClientMessage>();
        if (_backlog.TryGetValue(conversationId, out var backlog))
        {
            List<ClientMessageFragment> snapshot;
            lock (backlog)
            {
                snapshot = backlog.ToList();
            }

            foreach (var group in snapshot.GroupBy(f => f.Id))
            {
                var ordered = group.OrderBy(f => f.FragmentId).ToList();
                var text = string.Concat(ordered.Select(f => f.Text));
                if (text.StartsWith("Generating reply...", StringComparison.Ordinal))
                {
                    text = text.Replace("Generating reply...", string.Empty, StringComparison.Ordinal);
                }

                messages.Add(new ClientMessage(group.Key, ordered[0].Sender, text));
            }
        }

        return Task.FromResult<IList<ClientMessage>>(messages);
    }

    private static void AddLocalSubscriber(Guid conversationId, Action<ClientMessageFragment> callback)
    {
        var list = Subscribers.GetOrAdd(conversationId, _ => []);
        lock (list)
        {
            list.Add(callback);
        }
    }

    private static void RemoveLocalSubscriber(Guid conversationId, Action<ClientMessageFragment> callback)
    {
        if (Subscribers.TryGetValue(conversationId, out var list))
        {
            lock (list)
            {
                list.Remove(callback);
                if (list.Count == 0)
                {
                    Subscribers.TryRemove(conversationId, out _);
                }
            }
        }
    }
}
