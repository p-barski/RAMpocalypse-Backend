using System.Diagnostics.CodeAnalysis;
using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public class MatchmakingQueue
{
    private readonly LinkedList<Player> queue = [];
    private readonly Dictionary<string, LinkedListNode<Player>> index = [];
    private readonly Lock queueLock = new();

    public int Count
    {
        get
        {
            lock (queueLock)
            {
                return queue.Count;
            }
        }
    }

    public void Enqueue(Player player)
    {
        lock (queueLock)
        {
            if (index.ContainsKey(player.Id))
                return;

            var node = queue.AddLast(player);
            index[player.Id] = node;
        }
    }

    public bool TryDequeue([NotNullWhen(true)] out Player? player)
    {
        lock (queueLock)
        {
            if (queue.First is null)
            {
                player = default;
                return false;
            }

            var node = queue.First;
            queue.RemoveFirst();
            index.Remove(node.Value.Id);
            player = node.Value;
            return true;
        }
    }

    public bool Remove(Player player)
    {
        lock (queueLock)
        {
            if (!index.TryGetValue(player.Id, out var node))
                return false;

            queue.Remove(node);
            index.Remove(player.Id);
            return true;
        }
    }

    public bool Contains(Player player)
    {
        lock (queueLock)
        {
            return index.ContainsKey(player.Id);
        }
    }

    public void Clear()
    {
        lock (queueLock)
        {
            queue.Clear();
            index.Clear();
        }
    }
}
