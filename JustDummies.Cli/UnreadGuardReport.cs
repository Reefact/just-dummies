using JustDummies.GenAny;

namespace JustDummies.Cli;

/// <summary>The first guard the engine could not vouch for on a parameter, and where it sits (§6.1).</summary>
internal sealed record UnreadGuardReport(string? File, int Line, string Excerpt) {

    internal static UnreadGuardReport? Of(UnreadGuard? unread) {
        return unread is null ? null : new UnreadGuardReport(unread.File, unread.Line, unread.Excerpt);
    }

}
