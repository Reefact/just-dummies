using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     Loads a compiled emitted file and actually draws from it.
/// </summary>
/// <remarks>
///     The oracle the suite never had. Compiling proves the file binds; it says nothing about whether the
///     generator inside it produces a value, and a whole class of defect lives in that gap — a chain that is
///     legal, declarable and simply not what the guards said. Nothing decides that but the domain's own
///     constructor, which the emitted <c>Generate()</c> already calls: draw, and either a value comes back or
///     the invariant the engine claimed to read rejects it.
///     <para>
///         The draw is seeded, so a failure reproduces. It reaches the emitted generator because that generator
///         draws from the ambient context (ADR-0061) and the assembly loaded here binds to the very
///         <c>JustDummies.dll</c> this suite is running against.
///     </para>
/// </remarks>
internal static class EmittedAssembly {

    /// <summary>The seed every corpus draw runs under, so a red run is a red run again.</summary>
    private const int Seed = 20260822;

    /// <summary>
    ///     Draws <paramref name="count" /> values from <paramref name="generator" />, or says what stopped it.
    /// </summary>
    /// <returns>The failure, or null when every draw produced a value.</returns>
    internal static string? DrawFrom(CSharpCompilation compilation, string generator, int count) {
        return Attempt(compilation, generator, count)?.ToString();
    }

    /// <summary>
    ///     The same draw, with the thrown type kept apart from the text that renders it.
    /// </summary>
    /// <remarks>
    ///     What separates a refusal from a defect is WHICH exception came back: an
    ///     <c>AnyGenerationException</c> is the library declining a domain it cannot honour (ADR-0046), and
    ///     anything else — the domain's own <c>ArgumentException</c> above all — is a value the engine
    ///     produced and the constructor rejected. A caller that had to read that out of a rendered sentence
    ///     would be matching on prose.
    /// </remarks>
    internal static DrawFailure? Attempt(CSharpCompilation compilation, string generator, int count) {
        IReadOnlyList<string> errors = EmittedCodeCompiler.ErrorsIn(compilation);

        if (errors.Count > 0) { return new DrawFailure(kind: null, "it does not compile: " + string.Join("; ", errors.Take(3))); }

        using MemoryStream assembly = new();

        EmitResult emitted = compilation.Emit(assembly);

        if (!emitted.Success) {
            return new DrawFailure(kind: null,
                                   "it did not emit: " + string.Join("; ", emitted.Diagnostics
                                                                             .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                                                                             .Select(diagnostic => diagnostic.GetMessage())
                                                                             .Take(3)));
        }

        Type? type = Assembly.Load(assembly.ToArray()).GetType(generator);

        if (type is null) { return new DrawFailure(kind: null, $"the emitted assembly carries no {generator}."); }

        return Draw(type, count);
    }

    /// <summary>What stopped a draw: the thrown type when there was one, and the sentence to print.</summary>
    internal sealed class DrawFailure(string? kind, string text) {

        /// <summary>The name of the exception thrown, or null when nothing was thrown.</summary>
        internal string? Kind { get; } = kind;

        /// <inheritdoc />
        public override string ToString() {
            return text;
        }

    }

    /// <summary>
    ///     Construction and drawing are one question here, and the answer names which of the two failed.
    /// </summary>
    /// <remarks>
    ///     The emitted parameterless constructor runs the whole recipe, so a chain the library refuses is
    ///     refused there rather than at the first draw — which is precisely why a suite that only compiles sees
    ///     none of it.
    /// </remarks>
    private static DrawFailure? Draw(Type generator, int count) {
        MethodInfo? generate = generator.GetMethod("Generate", Type.EmptyTypes);

        if (generate is null) { return new DrawFailure(kind: null, $"{generator.Name} has no Generate()."); }

        using IDisposable seeded = Any.UseSeed(Seed);

        object instance;

        try {
            instance = Activator.CreateInstance(generator)!;
        } catch (TargetInvocationException invocation) {
            return new DrawFailure(KindOf(invocation.InnerException),
                                   "new " + generator.Name + "() threw " + Describe(invocation.InnerException));
        }

        for (int draw = 0; draw < count; draw++) {
            try {
                generate.Invoke(instance, parameters: null);
            } catch (TargetInvocationException invocation) {
                return new DrawFailure(KindOf(invocation.InnerException),
                                       $"draw {draw + 1} of {count} threw " + Describe(invocation.InnerException));
            }
        }

        return null;
    }

    private static string? KindOf(Exception? thrown) {
        return thrown?.GetType().Name;
    }

    private static string Describe(Exception? thrown) {
        return thrown is null ? "an unknown failure." : $"{thrown.GetType().Name}: {thrown.Message}";
    }

}
