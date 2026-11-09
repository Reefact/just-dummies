using EnumRepro.Domain;
using JustDummies;
using Xunit;

namespace EnumRepro.Tests;

public sealed class EnumScaffoldTests {

    [Fact]
    public void Bug_scaffolded_enum_generator_draws_an_undeclared_constant() {
        AnyCurrency generator = new AnyCurrency();
        HashSet<Currency> seen = new HashSet<Currency>();
        for (int i = 0; i < 200; i++) {
            seen.Add(generator.Generate());
        }
        Assert.Single(seen);
        Assert.False(Enum.IsDefined(seen.Single()));
    }

    [Fact]
    public void Library_enum_generator_draws_declared_members() {
        HashSet<Currency> seen = new HashSet<Currency>();
        for (int i = 0; i < 200; i++) {
            seen.Add(Any.Enum<Currency>().Generate());
        }
        Assert.All(seen, value => Assert.True(Enum.IsDefined(value)));
        Assert.True(seen.Count > 1);
    }

}
