using JustDummies;
using JustDummies.Xunit;
using Repro.Domain;
using Xunit;

namespace Repro.Tests;

public static class Arbitrary {
    public static AnyLeg Leg() {
        return new AnyLeg();
    }
}

public static class StaticDraws {
    public static readonly int Control = Any.Int32().Generate();                 // expect JD009
    public static readonly Leg ViaNew = new AnyLeg().Generate();                 // V1
    public static readonly Leg ViaArbitrary = Arbitrary.Leg().Generate();        // V2
    public static readonly Leg ViaAnyEntry = Any.Leg().Generate();               // V3
    public static readonly Leg ViaCombine = Any.Combine(Any.Int32(), Any.Int32(), (a, b) => new Leg(a, b)).Generate(); // V4
}

[Reproducible]
public class ConstructorDraws {
    private readonly int _control;
    private readonly Leg _viaNew;
    private readonly Leg _viaArbitrary;
    private readonly Leg _viaAnyEntry;
    private readonly Leg _viaCombine;

    public ConstructorDraws() {
        _control = Any.Int32().Generate();                                       // expect JD007
        _viaNew = new AnyLeg().Generate();                                       // V5
        _viaArbitrary = Arbitrary.Leg().Generate();                              // V6
        _viaAnyEntry = Any.Leg().Generate();                                     // V7
        _viaCombine = Any.Combine(Any.Int32(), Any.Int32(), (a, b) => new Leg(a, b)).Generate(); // V8
    }

    [Fact]
    public void Uses_the_fields() {
        Assert.NotNull(_viaNew);
        Assert.NotNull(_viaArbitrary);
        Assert.NotNull(_viaAnyEntry);
        Assert.NotNull(_viaCombine);
        Assert.True(_control == _control);
    }
}

public class DistinctProbes {
    [Fact]
    public void Control_as() {
        List<Leg> legs = Any.ListOf(Any.Int32().Between(1, 2).As(v => new Leg(v, v))).Distinct().WithCount(6).Generate();   // expect JD028
        Assert.Equal(6, legs.Count);
    }

    [Fact]
    public void Via_new() {
        List<Leg> legs = Any.ListOf(new AnyLeg()).Distinct().WithCount(6).Generate();          // V9
        Assert.Equal(6, legs.Count);
    }

    [Fact]
    public void Via_any_entry() {
        List<Leg> legs = Any.ListOf(Any.Leg()).Distinct().WithCount(6).Generate();             // V10
        Assert.Equal(6, legs.Count);
    }

    [Fact]
    public void Via_inline_combine() {
        List<Leg> legs = Any.ListOf(Any.Combine(Any.Int32().Between(1, 2), Any.Int32().Between(1, 2), (a, b) => new Leg(a, b))).Distinct().WithCount(6).Generate(); // V11
        Assert.Equal(6, legs.Count);
    }

    [Fact]
    public void Via_combine_local() {
        IAny<Leg> anyLeg = Any.Combine(Any.Int32().Between(1, 2), Any.Int32().Between(1, 2), (a, b) => new Leg(a, b));
        List<Leg> legs = Any.ListOf(anyLeg).Distinct().WithCount(6).Generate();                 // V12
        Assert.Equal(6, legs.Count);
    }
}
