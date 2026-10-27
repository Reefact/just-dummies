using JustDummies;
using Repro.Domain;
using Xunit;

namespace Repro.Tests;

public sealed class MessageTests {
    [Fact]
    public void As_failure_message_escapes_control_characters() {
        AnyGenerationException exception = Assert.Throws<AnyGenerationException>(
            () => Any.String().WithChars("a\0\r\f\u001b").WithLength(6).As(v => new Code(v)).Generate());
        string message = exception.Message;
        Assert.DoesNotContain(message, character => character < ' ' && character != '\n');
    }

    [Fact]
    public void Combine_failure_message_escapes_control_characters() {
        AnyGenerationException exception = Assert.Throws<AnyGenerationException>(
            () => Any.Combine(Any.String().WithChars("\0").WithLength(2), Any.String().WithChars("\r").WithLength(2), (a, b) => new Code(a + b)).Generate());
        Assert.DoesNotContain(exception.Message, character => character < ' ' && character != '\n');
    }
}
