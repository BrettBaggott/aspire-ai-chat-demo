public interface IConversationStore
{
    IReadOnlyList<Conversation> GetAll();
    Conversation? Get(Guid id);
    Conversation Create(string name);
    void AddMessage(Guid conversationId, ConversationChatMessage message);
    bool Delete(Guid id);
}
