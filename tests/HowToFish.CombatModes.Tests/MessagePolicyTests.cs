using HowToFish.CombatModes.Core;
using Xunit;

namespace HowToFish.CombatModes.Tests;

public class MessagePolicyTests
{
    [Theory]
    [InlineData(MessageKind.State, true, false, true)]   // host -> guest
    [InlineData(MessageKind.State, true, true, false)]   // echo on the host
    [InlineData(MessageKind.State, false, false, false)] // forged STATE from a guest
    [InlineData(MessageKind.Hello, false, true, true)]   // guest -> host
    [InlineData(MessageKind.Hello, false, false, false)] // echo on the guest
    [InlineData(MessageKind.Hello, true, true, false)]   // HELLO from the host itself
    [InlineData(MessageKind.Rematch, false, true, true)]
    [InlineData(MessageKind.Rematch, true, false, false)]
    public void IsAccepted_FollowsHostAuthority(MessageKind kind, bool senderIsOwner, bool localIsHost, bool expected)
    {
        Assert.Equal(expected, MessagePolicy.IsAccepted(kind, senderIsOwner, localIsHost));
    }
}
