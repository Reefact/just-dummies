using JustDummies;
using Xunit;

namespace InfoInvisible.Tests;

public class InfoProbe {
    [Fact]
    public void Undeclared_string_length_is_reported_as_JD030() {
        string name = Any.String().Generate(); // JD030 expected (Info, on by default)
        string code = Any.String().OneOf("abc", "de").WithLength(3).Generate(); // JD029 expected (Info, on by default)
        Assert.NotNull(name);
        Assert.Equal(3, code.Length);
    }
}
