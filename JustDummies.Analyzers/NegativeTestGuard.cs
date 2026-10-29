using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JustDummies.Analyzers;

/// <summary>
///     Suppresses a diagnostic on the shape a test uses to assert that a call fails: the offending expression is the
///     <b>entire</b> body of an expression-bodied lambda handed to another call — <c>Assert.Throws(() =&gt; illegal)</c>,
///     <c>Check.ThatCode(() =&gt; illegal)</c>, <c>Should.Throw(() =&gt; illegal)</c>.
/// </summary>
/// <remarks>
///     Deliberately framework-agnostic: it names no assertion library, so it covers the ones this repository uses and
///     the ones a consumer brings. It is also deliberately narrow — the expression must be the whole lambda body, so
///     arrange code inside <c>Any.Reproducibly(() =&gt; { ... })</c> stays reported, the call there being one statement
///     of a block rather than the body itself.
/// </remarks>
internal static class NegativeTestGuard {

    public static bool IsSoleBodyOfLambdaArgument(SyntaxNode expression) {
        if (expression.Parent is not LambdaExpressionSyntax lambda) { return false; }
        if (lambda.Body != expression) { return false; }

        return lambda.Parent is ArgumentSyntax;
    }

    /// <summary>
    ///     Whether <paramref name="call" /> is a link of a chain whose outermost call is the sole body of a lambda
    ///     argument — the call itself, or one the chain goes on from, as in <c>Assert.Throws(() =&gt; illegal.Generate())</c>.
    /// </summary>
    /// <remarks>
    ///     For a rule that judges one call of a chain rather than the whole of it. A rule judging the outermost call
    ///     already asks <see cref="IsSoleBodyOfLambdaArgument" /> about the whole chain, and the two must agree on one
    ///     negative test (issue #323). Only the receiver side is climbed: a chain passed as an argument to another call
    ///     inside the lambda is not the expression the test asserts on.
    /// </remarks>
    public static bool IsLinkOfSoleBodyOfLambdaArgument(SyntaxNode call) {
        SyntaxNode outermost = call;
        while (outermost.Parent is MemberAccessExpressionSyntax access && access.Expression == outermost
            && access.Parent is InvocationExpressionSyntax invocation && invocation.Expression == access) {
            outermost = invocation;
        }

        return IsSoleBodyOfLambdaArgument(outermost);
    }

}
