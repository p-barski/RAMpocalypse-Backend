namespace RAMpocalypse.Tests;

public sealed class TestTimeProvider(long utcMilliseconds) : TimeProvider
{
    private DateTimeOffset utcNow = DateTimeOffset.FromUnixTimeMilliseconds(utcMilliseconds);

    public void SetUtcMilliseconds(long utcMilliseconds) =>
        utcNow = DateTimeOffset.FromUnixTimeMilliseconds(utcMilliseconds);

    public override DateTimeOffset GetUtcNow() => utcNow;
}
