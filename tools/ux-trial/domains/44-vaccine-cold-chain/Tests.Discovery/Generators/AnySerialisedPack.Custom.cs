// AnySerialisedPack.Custom.cs: written by hand, never touched by `any generate --force`.
using System;

using JustDummies;

namespace Serialisation.Domain;

public sealed partial class AnySerialisedPack {

    /// <summary>The expiry dates a pack may carry, for a generator that builds packs of its own.</summary>
    internal static AnyDateOnly ValidExpiresOn() { return AnyValidExpiresOn(); }

}
