#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.Xunit.UnitTests;

/// <summary>
///     The adapter's hooks, driven directly rather than through the xUnit pipeline.
/// </summary>
/// <remarks>
///     <para>
///         Everything the surrounding suite asserts is observed from <i>inside</i> a decorated test, which is the right
///         way to prove that pinning works — but it leaves the hooks' own defensive behaviour unobserved: the test never
///         sees <c>After</c> run, so it cannot tell whether the scope was closed or merely abandoned, nor what happens
///         when <c>After</c> runs without a matching <c>Before</c>.
///     </para>
///     <para>
///         Both hooks ignore their two parameters entirely, so they are passed as <c>null</c> here: naming a real
///         <c>MethodInfo</c> and a fake <c>IXunitTest</c> would suggest they matter.
///     </para>
/// </remarks>
[TestSubject(typeof(ReproducibleAttribute))]
public sealed class ReproducibleAttributeHookTests {

    #region Statics members declarations

    private static (int, string) Batch() {
        return (Any.Int32().Generate(), Any.String().NonEmpty().Generate());
    }

    #endregion

    [Fact(DisplayName = "A declared seed is the seed the attribute reports.")]
    public void ADeclaredSeedIsReadBack() {
        // The property is what a reader sets and what the replay snippet echoes; nothing else asserted that setting
        // it had any effect at all.
        ReproducibleAttribute attribute = new() { Seed = 1234 };

        Check.That(attribute.Seed).IsEqualTo(1234);
    }

    [Fact(DisplayName = "An undeclared seed reads as zero.")]
    public void AnUndeclaredSeedReadsAsZero() {
        Check.That(new ReproducibleAttribute().Seed).IsEqualTo(0);
    }

    [Fact(DisplayName = "The after-hook is a no-op when no scope was opened for it.")]
    public void TheAfterHookToleratesAMissingScope() {
        // xUnit is not obliged to have run Before — a failure in another hook can skip it — and the after-hook must
        // survive that rather than take the whole test run down with a NullReferenceException.
        ReproducibleAttribute attribute = new();

        Check.ThatCode(() => attribute.After(null!, null!)).DoesNotThrow();
    }

    [Fact(DisplayName = "The after-hook closes the scope it opened, releasing the ambient source.")]
    public void TheAfterHookClosesTheScope() {
        // What seed 555 produces on its second draw. If the scope were left open, the draw after the hook would
        // continue that sequence and match; a closed scope hands the ambient source back and it will not.
        (int, string) secondOfTheSeededRun;
        using (Any.UseSeed(555)) {
            Batch();
            secondOfTheSeededRun = Batch();
        }

        ReproducibleAttribute attribute = new() { Seed = 555 };
        attribute.Before(null!, null!);
        Batch();
        attribute.After(null!, null!);

        Check.That(Batch()).IsNotEqualTo(secondOfTheSeededRun);
    }

    [Fact(DisplayName = "Nested scopes unwind in order, restoring the outer seed.")]
    public void NestedScopesUnwindInOrder() {
        // The method, class and assembly levels nest, and xUnit closes them in reverse. Closing the inner one must
        // restore the outer seed rather than the unpinned source.
        ReproducibleAttribute outer = new() { Seed = 111 };
        ReproducibleAttribute inner = new() { Seed = 222 };

        (int, string) expectedSecondOfOuter;
        using (Any.UseSeed(111)) {
            Batch();
            expectedSecondOfOuter = Batch();
        }

        outer.Before(null!, null!);
        try {
            Batch();

            inner.Before(null!, null!);
            Batch();
            inner.After(null!, null!);

            Check.That(Batch()).IsEqualTo(expectedSecondOfOuter);
        } finally {
            outer.After(null!, null!);
        }
    }

    [Fact(DisplayName = "A cancelled test is told its seed through its output, once.")]
    public void ACancelledTestIsToldItsSeedOnce() {
        // Issue #230: a test that times out is failed, and its output closed, before the after-hook runs. xUnit
        // cancels the test's token first, so that is where the report is written -- and the after-hook, seeing the
        // failure too, must not write it a second time.
        RecordingOutput output = new();
        using CancellationTokenSource cancellation = new();
        ReproducibleAttribute.Scope scope = new(NoHandle.Instance, 1234, null);

        scope.ReportOnCancellation(cancellation.Token, output);
        scope.Activate();
        cancellation.Cancel();

        Check.That(output.Lines).CountIs(1);
        Check.That(output.Lines[0]).Contains("seeded with 1234", "[Reproducible(Seed = 1234)]");
        Check.That(scope.TryClaimReport()).IsFalse();
    }

    [Fact(DisplayName = "A test that is never cancelled writes nothing from the cancellation.")]
    public void ATestNeverCancelledWritesNothing() {
        // The after-hook stops listening before it decides; a cancellation after that -- the run tearing down --
        // must not reach a test that already finished, and the after-hook keeps the report for itself.
        RecordingOutput output = new();
        using CancellationTokenSource cancellation = new();
        ReproducibleAttribute.Scope scope = new(NoHandle.Instance, 1234, null);

        scope.ReportOnCancellation(cancellation.Token, output);
        scope.Activate();
        scope.StopListening();
        cancellation.Cancel();

        Check.That(output.Lines).IsEmpty();
        Check.That(scope.TryClaimReport()).IsTrue();
    }

    [Fact(DisplayName = "A token already cancelled when the test starts reports nothing.")]
    public void AnAlreadyCancelledTokenReportsNothing() {
        // That token belongs to a run being torn down before the test drew anything: there is no run to replay.
        RecordingOutput output = new();
        using CancellationTokenSource cancellation = new();
        ReproducibleAttribute.Scope scope = new(NoHandle.Instance, 1234, null);

        cancellation.Cancel();
        scope.ReportOnCancellation(cancellation.Token, output);

        Check.That(output.Lines).IsEmpty();
    }

    [Fact(DisplayName = "A report the output no longer takes is dropped, not thrown into the cancellation.")]
    public void AClosedOutputDoesNotThrowIntoTheCancellation() {
        // An exception from a cancellation callback surfaces in whoever called Cancel -- here, xUnit's own timeout.
        ReproducibleAttribute.Scope scope = new(NoHandle.Instance, 1234, null);
        using CancellationTokenSource cancellation = new();

        scope.ReportOnCancellation(cancellation.Token, new ClosedOutput());
        scope.Activate();

        Check.ThatCode(() => cancellation.Cancel()).DoesNotThrow();
    }

    [Fact(DisplayName = "A cancellation while the level is being set up reports nothing, and leaves the report open.")]
    public void ACancellationBeforeTheLevelIsInPlaceReportsNothing() {
        // Registering runs the callback at once on a token cancelled just after it was checked. At that point the
        // level is not in place, so its seed served no draw: nothing is written, and the after-hook still decides.
        RecordingOutput output = new();
        using CancellationTokenSource cancellation = new();
        ReproducibleAttribute.Scope scope = new(NoHandle.Instance, 1234, null);

        scope.ReportOnCancellation(cancellation.Token, output);
        cancellation.Cancel();
        scope.Activate();

        Check.That(output.Lines).IsEmpty();
        Check.That(scope.TryClaimReport()).IsTrue();
    }

    #region Nested types

    private sealed class NoHandle : IDisposable {

        internal static readonly NoHandle Instance = new();

        public void Dispose() { }

    }

    private sealed class RecordingOutput : ITestOutputHelper {

        internal List<string> Lines { get; } = [];

        public string Output => string.Join(Environment.NewLine, Lines);

        public void Write(string message) { Lines.Add(message); }

        public void Write(string format, params object[] args) { Lines.Add(string.Format(format, args)); }

        public void WriteLine(string message) { Lines.Add(message); }

        public void WriteLine(string format, params object[] args) { Lines.Add(string.Format(format, args)); }

    }

    private sealed class ClosedOutput : ITestOutputHelper {

        public string Output => "";

        public void Write(string message) { throw new InvalidOperationException("There is no currently active test."); }

        public void Write(string format, params object[] args) { throw new InvalidOperationException("There is no currently active test."); }

        public void WriteLine(string message) { throw new InvalidOperationException("There is no currently active test."); }

        public void WriteLine(string format, params object[] args) { throw new InvalidOperationException("There is no currently active test."); }

    }

    #endregion

}
