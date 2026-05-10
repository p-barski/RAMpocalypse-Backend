namespace RAMpocalypse.Server.Services;

public interface ILongLivedAttacksCleaner
{
    void PruneExpiredAttacks();
}
