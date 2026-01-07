using System.Collections.Concurrent;

public class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<Guid, Conversation> _conversations = new();

    public IReadOnlyList<Conversation> GetAll() => _conversations.Values.ToList();

    public Conversation? Get(Guid id) =>
        _conversations.TryGetValue(id, out var conversation) ? conversation : null;

    public Conversation Create(string name)
    {
        var conversation = new Conversation
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Messages = []
        };

        _conversations[conversation.Id] = conversation;
        return conversation;
    }

    public void AddMessage(Guid conversationId, ConversationChatMessage message)
    {
        if (_conversations.TryGetValue(conversationId, out var conversation))
        {
            conversation.Messages.Add(message);
        }
    }

    public bool Delete(Guid id) => _conversations.TryRemove(id, out _);
}
