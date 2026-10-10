namespace HowToFish.Duel1v1.Core;

public enum Side
{
    Host = 0,
    Guest = 1,
}

public static class SideExtensions
{
    public static Side Other(this Side side) => side == Side.Host ? Side.Guest : Side.Host;
}
