using HowToFish.CombatModes.Core;
using Xunit;

namespace HowToFish.CombatModes.Tests;

public class MessagePolicyTests
{
    [Theory]
    [InlineData(MessageKind.State, true, false, true)]   // hôte -> invité
    [InlineData(MessageKind.State, true, true, false)]   // écho chez l'hôte
    [InlineData(MessageKind.State, false, false, false)] // faux STATE d'un invité
    [InlineData(MessageKind.Hello, false, true, true)]   // invité -> hôte
    [InlineData(MessageKind.Hello, false, false, false)] // écho chez l'invité
    [InlineData(MessageKind.Hello, true, true, false)]   // HELLO de l'hôte lui-même
    [InlineData(MessageKind.Rematch, false, true, true)]
    [InlineData(MessageKind.Rematch, true, false, false)]
    public void IsAccepted_FollowsHostAuthority(MessageKind kind, bool senderIsOwner, bool localIsHost, bool expected)
    {
        Assert.Equal(expected, MessagePolicy.IsAccepted(kind, senderIsOwner, localIsHost));
    }
}
