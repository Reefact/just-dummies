using JustDummies;
using TodoWording.Domain;
using Xunit;

namespace TodoWording.Tests;

public class TodoWordingTests {

    [Fact]
    public void Resolving_a_to_verify_base_with_a_pattern_by_deleting_one_line() {
        AnalyteCode anyCode = new AnyAnalyteCode().Generate();

        Assert.NotNull(anyCode.Value);
    }

}
