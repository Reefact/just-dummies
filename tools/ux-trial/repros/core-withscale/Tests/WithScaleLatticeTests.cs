using System.Numerics;
using JustDummies;
using Xunit;

namespace CoreWithScale.Tests;

public sealed class WithScaleLatticeTests {

    private readonly ITestOutputHelper _output;

    public WithScaleLatticeTests(ITestOutputHelper output) {
        _output = output;
    }

    // Exact membership in the value lattice: value is a multiple of 10^-n.
    private static bool IsMultipleOfTenToMinus(decimal value, int n) {
        int[] bits = decimal.GetBits(value);
        BigInteger mantissa = new BigInteger((uint)bits[0]) | (new BigInteger((uint)bits[1]) << 32) | (new BigInteger((uint)bits[2]) << 64);
        int scale = (bits[3] >> 16) & 0xFF;
        if (scale <= n) { return true; }
        return mantissa % BigInteger.Pow(10, scale - n) == 0;
    }

    private void Check(string label, IAny<decimal> any, int n, decimal min, decimal max) {
        Dictionary<int, int> scales = new Dictionary<int, int>();
        int offLattice = 0;
        int outOfRange = 0;
        decimal? example = null;
        for (int i = 0; i < 500; i++) {
            decimal value = any.Generate();
            int scale = value.Scale;
            scales[scale] = scales.TryGetValue(scale, out int c) ? c + 1 : 1;
            if (!IsMultipleOfTenToMinus(value, n)) { offLattice++; example = value; }
            if (value < min || value > max) { outOfRange++; }
        }
        string histogram = string.Join(", ", scales.OrderBy(p => p.Key).Select(p => $"scale {p.Key}: {p.Value}"));
        _output.WriteLine($"{label} -> offLattice={offLattice} outOfRange={outOfRange} [{histogram}] {example}");
        Assert.Equal(0, offLattice);
        Assert.Equal(0, outOfRange);
    }

    [Fact]
    public void Near_max_value_scale_2() {
        Check("GreaterThanOrEqualTo(7.9e27m).WithScale(2)", Any.Decimal().GreaterThanOrEqualTo(7.9e27m).WithScale(2), 2, 7.9e27m, decimal.MaxValue);
        Check("Between(Max-1, Max).WithScale(2)", Any.Decimal().Between(decimal.MaxValue - 1m, decimal.MaxValue).WithScale(2), 2, decimal.MaxValue - 1m, decimal.MaxValue);
        Check("Between(1e27m, 7e28m).WithScale(2)", Any.Decimal().Between(1e27m, 7e28m).WithScale(2), 2, 1e27m, 7e28m);
    }

    [Fact]
    public void Scale_28() {
        Check("Between(0, 1).WithScale(28)", Any.Decimal().Between(0m, 1m).WithScale(28), 28, 0m, 1m);
        Check("Between(999_999, 2_000_000).WithScale(28)", Any.Decimal().Between(999_999m, 2_000_000m).WithScale(28), 28, 999_999m, 2_000_000m);
    }

    [Fact]
    public void Allow_list_and_single_value() {
        decimal fromAllowList = Any.Decimal().WithScale(1).OneOf(2.50m).Generate();
        _output.WriteLine($"WithScale(1).OneOf(2.50m) -> {fromAllowList} (Scale {fromAllowList.Scale})");
        decimal single = Any.Decimal().Between(20m, 20m).WithScale(1).Generate();
        _output.WriteLine($"Between(20, 20).WithScale(1) -> {single} (Scale {single.Scale})");
        Assert.True(IsMultipleOfTenToMinus(fromAllowList, 1));
        Assert.True(IsMultipleOfTenToMinus(single, 1));
    }

    [Fact]
    public void Off_lattice_single_value_and_allow_list_are_refused() {
        Exception single = Record.Exception(() => Any.Decimal().Between(1.234m, 1.234m).WithScale(2).Generate());
        _output.WriteLine($"Between(1.234, 1.234).WithScale(2) -> {single?.GetType().Name}: {single?.Message}");
        Exception allow = Record.Exception(() => Any.Decimal().WithScale(1).OneOf(2.55m).Generate());
        _output.WriteLine($"WithScale(1).OneOf(2.55m) -> {allow?.GetType().Name}: {allow?.Message}");
        Exception order = Record.Exception(() => Any.Decimal().OneOf(2.55m).WithScale(1).Generate());
        _output.WriteLine($"OneOf(2.55m).WithScale(1) -> {order?.GetType().Name}: {order?.Message}");
        Assert.NotNull(single);
        Assert.NotNull(allow);
        Assert.NotNull(order);
    }
}
