using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     A nullable parameter draws <c>null</c> one draw in ten unless a guard refuses it — by default only: a
///     refinement describes a value, and <c>Without…()</c> is how a test asks for <c>null</c> (§5.2, ADR-0117, #411).
/// </summary>
/// <remarks>
///     The constructor declares <c>null</c> valid by the parameter's type. Never drawing it never explored a value the
///     type itself names; a test that fails on it was depending on the parameter, and the seed replays it.
/// </remarks>
public sealed class NullDrawTests {

    private const string Domain = """
                                  namespace Shop.Domain;

                                  public sealed class Subject {

                                      public Subject(string? note, int? priority) {
                                          Note     = note;
                                          Priority = priority;
                                      }

                                      public string? Note { get; }

                                      public int? Priority { get; }

                                  }
                                  """;

    [Theory(DisplayName = "A nullable parameter draws null by default, and a refinement goes through the hop that never does.")]
    [InlineData("string?", "Any.String().OrNull()", "")]
    [InlineData("Customer?", "new AnyCustomer().OrNull()", "")]
    [InlineData("int?", "Any.Int32().OrNull()", ".AsNullable()")]
    [InlineData("DateTime?", "Any.DateTime().OrNull()", ".AsNullable()")]
    [InlineData("OrderStatus?", "Any.Enum<OrderStatus>().OrNull()", ".AsNullable()")]
    public void ANullableParameterDrawsNullByDefault(string parameterType, string expected, string refinement) {
        ScaffoldedParameter parameter = Subject.GuardedBy(parameterType, string.Empty);

        Check.That(parameter.DrawsNull).IsTrue();
        Check.That(parameter.Expression).IsEqualTo(expected);
        Check.That(parameter.Conversion).IsEqualTo(refinement);
    }

    [Theory(DisplayName = "A guard that refuses null keeps the default a value.")]
    [InlineData("string?", "if (value is null) { throw new ArgumentNullException(nameof(value)); }", "Any.String()")]
    [InlineData("string?", "if (value == null) { throw new ArgumentNullException(nameof(value)); }", "Any.String()")]
    [InlineData("string?", "ArgumentNullException.ThrowIfNull(value);", "Any.String()")]
    [InlineData("string?", "kept = value ?? throw new ArgumentNullException(nameof(value));", "Any.String()")]
    [InlineData("string?", "if (string.IsNullOrEmpty(value)) { throw new ArgumentException(nameof(value)); }", "Any.String().NonEmpty()")]
    [InlineData("string?", "ArgumentException.ThrowIfNullOrWhiteSpace(value);", "Any.String().NotBlank()")]
    [InlineData("string?", "if (value.Length > 10) { throw new ArgumentException(nameof(value)); }", "Any.String().WithMaxLength(10)")]
    [InlineData("int?", "if (value is null) { throw new ArgumentNullException(nameof(value)); }", "Any.Int32().AsNullable()")]
    public void AGuardThatRefusesNullKeepsTheDefaultAValue(string parameterType, string guard, string expected) {
        ScaffoldedParameter parameter = Subject.GuardedBy(parameterType, guard);

        Check.That(parameter.DrawsNull).IsFalse();
        Check.That(parameter.Expression).IsEqualTo(expected);
    }

    // The check sits below the first write to state and tests the member rather than the parameter, so it narrows
    // nothing — and still throws on the null a nullable parameter would draw.
    [Fact(DisplayName = "A null-check on a member holding the parameter unchanged refuses null too.")]
    public void ANullCheckOnAPlainCopyRefusesNull() {
        ScaffoldedParameter parameter = Subject.Scaffold("""
                                                         namespace Shop.Domain;

                                                         public sealed class Subject {

                                                             public Subject(string? name) {
                                                                 Name = name;

                                                                 if (Name is null) { throw new System.ArgumentNullException(nameof(name)); }
                                                             }

                                                             public string? Name { get; }

                                                         }
                                                         """).Plan!.Parameters.Single();

        Check.That(parameter.DrawsNull).IsFalse();
        Check.That(parameter.RequiresVerification).IsFalse();
    }

    // A lifted comparison lets null through — `null <= 0` is false — so a bound on a nullable value type says
    // nothing about null, and the default still draws it.
    [Fact(DisplayName = "A bound on a nullable value type does not refuse null.")]
    public void ABoundOnANullableValueTypeDoesNotRefuseNull() {
        ScaffoldedParameter parameter = Subject.GuardedBy("int?", "if (value <= 0) { throw new ArgumentOutOfRangeException(nameof(value)); }");

        Check.That(parameter.Expression).IsEqualTo("Any.Int32().Positive().OrNull()");
    }

    [Fact(DisplayName = "Without… pins null, beside the two With… overloads, only where the default draws it.")]
    public void WithoutPinsNullOnlyWhereTheDefaultDrawsIt() {
        string emitted = Subject.Scaffold("""
                                          namespace Shop.Domain;

                                          public sealed class Subject {
                                              public Subject(string? note, int? priority, string name, string? code) {
                                                  System.ArgumentNullException.ThrowIfNull(code);
                                              }
                                          }
                                          """).File!.SourceText;

        Check.That(emitted).Contains("= note.OrNull();",
                                     "= priority.OrNull();",
                                     "copy._priority       = copy._priorityRecipe.AsNullable();",
                                     "public AnySubject WithoutNote() {",
                                     "copy._note = Any.Fixed<string?>(null);",
                                     "public AnySubject WithoutPriority() {",
                                     "copy._priority = Any.Fixed<int?>(null);");
        Check.That(emitted).Not.Contains("WithoutName", "WithoutCode", "code.OrNull()");
    }

    [Fact(DisplayName = "The scaffold draws null by default, never after a refinement, and always through Without….")]
    public void TheScaffoldDrawsNullByDefaultOnly() {
        const string probe = """
                             using Shop.Domain;

                             namespace Shop.Tests;

                             public static class Probe {

                                 public static int NullNotes(int draws) {
                                     return Count(new AnySubject(), draws);
                                 }

                                 public static int NullNotesOnceRefined(int draws) {
                                     return Count(new AnySubject().WithNote(n => n.WithMaxLength(5)), draws);
                                 }

                                 public static bool NoteWithout() {
                                     return new AnySubject().WithoutNote().Generate().Note is null;
                                 }

                                 private static int Count(AnySubject subjects, int draws) {
                                     int seen = 0;

                                     for (int draw = 0; draw < draws; draw++) {
                                         if (subjects.Generate().Note is null) { seen++; }
                                     }

                                     return seen;
                                 }

                             }
                             """;

        string            emitted     = Subject.Scaffold(Domain).File!.SourceText;
        CSharpCompilation compilation = EmittedCodeCompiler.CompileAllWith([emitted, probe], Domain);

        Check.That(EmittedCodeCompiler.ErrorsIn(compilation)).IsEmpty();

        Type probes = Loaded(compilation).GetType("Shop.Tests.Probe")!;

        using IDisposable seeded = Any.UseSeed(20261101);

        int byDefault = (int)probes.GetMethod("NullNotes")!.Invoke(null, [2000])!;
        int refined   = (int)probes.GetMethod("NullNotesOnceRefined")!.Invoke(null, [2000])!;

        // One draw in ten: well clear of both never and half the time, which is all a rate check on a seeded draw
        // can honestly say — the rate itself is the library's, and its property suite holds it.
        Check.That(byDefault).IsStrictlyGreaterThan(100).And.IsStrictlyLessThan(400);
        Check.That(refined).IsEqualTo(0);
        Check.That((bool)probes.GetMethod("NoteWithout")!.Invoke(null, [])!).IsTrue();
    }

    private static Assembly Loaded(CSharpCompilation compilation) {
        using MemoryStream assembly = new();

        EmitResult emitted = compilation.Emit(assembly);

        Check.That(emitted.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsEmpty();

        return Assembly.Load(assembly.ToArray());
    }

}
