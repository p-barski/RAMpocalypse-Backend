namespace RAMpocalypse.Server.Services;

public enum ChatMessageType
{
    Global = 0,
    Lobby = 1,
}

public class ChatMessage
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public ChatMessageType Type { get; set; } = ChatMessageType.Global;
    public string OwnerId { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
