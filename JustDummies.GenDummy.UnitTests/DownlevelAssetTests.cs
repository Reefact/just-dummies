using NFluent;

namespace JustDummies.GenDummy.UnitTests;

/// <summary>
///     ADR-0059, proved rather than asserted: the same parameter, the library's two assets, two answers.
/// </summary>
/// <remarks>
///     <c>DateOnly</c>, <c>TimeOnly</c>, <c>Int128</c>, <c>UInt128</c> and <c>Half</c> do not exist below
///     .NET 8, so neither do their factories on the <c>netstandard2.0</c> asset. An engine that emitted
///     <c>Dummy.DateOnly()</c> from a table rather than from the compilation would hand a downlevel developer a
///     file that does not compile — and would do it on exactly the projects least able to diagnose it.
///     <para>
///         The row that stays resolved on both assets is what keeps this test honest: without it, an engine
///         that resolved nothing at all would pass.
///     </para>
/// </remarks>
public sealed class DownlevelAssetTests {

    [Theory(DisplayName = "A modern generator resolves on the net8.0 asset.")]
    [InlineData("DateOnly", "Dummy.DateOnly()")]
    [InlineData("TimeOnly", "Dummy.TimeOnly()")]
    [InlineData("Int128", "Dummy.Int128()")]
    [InlineData("UInt128", "Dummy.UInt128()")]
    [InlineData("Half", "Dummy.Half()")]
    public void AModernGeneratorResolvesOnTheModernAsset(string parameterType, string expected) {
        Check.That(Subject.ExpressionFor(parameterType)).IsEqualTo(expected);
    }

    [Theory(DisplayName = "The same generator is absent on the netstandard2.0 asset, and the parameter stays open.")]
    [InlineData("DateOnly")]
    [InlineData("TimeOnly")]
    [InlineData("Int128")]
    [InlineData("UInt128")]
    [InlineData("Half")]
    public void TheSameGeneratorIsAbsentOnTheDownlevelAsset(string parameterType) {
        Check.That(Subject.ExpressionFor(parameterType, downlevel: true)).IsNull();
    }

    [Theory(DisplayName = "A generator both assets carry resolves on both.")]
    [InlineData("string", "Dummy.String().NonEmpty()")]
    [InlineData("DateTime", "Dummy.DateTime()")]
    public void AGeneratorBothAssetsCarryResolvesOnBoth(string parameterType, string expected) {
        Check.That(Subject.ExpressionFor(parameterType)).IsEqualTo(expected);
        Check.That(Subject.ExpressionFor(parameterType, downlevel: true)).IsEqualTo(expected);
    }

}
