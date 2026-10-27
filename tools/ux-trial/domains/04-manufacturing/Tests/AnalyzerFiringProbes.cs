using JustDummies;
using Manufacturing.Domain;

namespace Manufacturing.Tests;

// Never run: the same chains as AnalyzerProbes.cs outside Assert.Throws, where the rules do fire.
public static class AnalyzerFiringProbes {
    public static void Run() {
        int a = Any.Int32().Between(1, 10).MultipleOf(50).Generate();
        HashSet<MachineState> b = Any.SetOf(Any.Enum<MachineState>()).WithCount(7).Generate();
        ShiftKind c = Any.Enum<ShiftKind>().OneOf((ShiftKind)0).Generate();
        string d = Any.String().StartingWith("LYN-").WithLength(3).Generate();
        Action e = () => Any.Int32().Between(1, 10).MultipleOf(50).Generate();
        Func<object> f = () => Any.Int32().Between(1, 10).MultipleOf(50);
    }
}
