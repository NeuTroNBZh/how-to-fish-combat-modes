using System.Collections.Generic;

namespace HowToFish.CombatModes.Core;

public sealed record Kit(string Name, IReadOnlyList<string> Items);
