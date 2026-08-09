namespace JustDummies.Cli;

/// <summary>
///     The process exit codes the tool returns.
/// </summary>
/// <remarks>
///     <see cref="Success" /> and the failure code of the specification's §7 belong to <c>generate</c>, and
///     describe how a scaffolding run ended. <see cref="Usage" /> belongs to the command line itself — it says the
///     tool never got as far as running anything — which is why it is a third value rather than a reuse of §7's
///     <c>1</c>: an invocation that could not start is not a scaffolding failure.
/// </remarks>
internal static class ExitCode {

    /// <summary>The command ran and did what it was asked.</summary>
    internal const int Success = 0;

    /// <summary>The command line was not understood, or names something this build cannot do.</summary>
    internal const int Usage = 2;

}
