using System;
using System.Collections.Generic;

using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     The pre-write name check of §4.5: an entry-point file that would declare a name already borne is refused
///     before it is written, naming both declarations.
/// </summary>
/// <remarks>
///     The case is <c>Voice</c> beside <c>VoiceEntry</c>: <c>--entry-point any</c> declares
///     <c>AnyVoiceEntry</c> for the first, which is the generator name of the second. Found by the second
///     developer-UX trial (CLI-11), where the run reported success and the project stopped compiling.
/// </remarks>
public sealed class EntryPointNameCheckTests {

    private const string Voices = """
                                  public sealed class VoiceEntry {
                                      public VoiceEntry(int pitch) { }
                                  }

                                  public sealed class Voice {
                                      public Voice(int number, IReadOnlyList<VoiceEntry> entries) { }
                                  }
                                  """;

    private static readonly ScaffoldOptions OnAny = ScaffoldOptions.Default.WithEntryPoint(EntryPointOptions.OnAny);

    [Fact(DisplayName = "An entry class the project already declares is refused, naming both declarations.")]
    public void AnEntryClassTheProjectAlreadyDeclaresIsRefused() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject { public Subject(int one) { } }

                                                   public sealed class AnySubjectEntry { }
                                                   """,
                                                   OnAny);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.EntryPointNameTaken);
        Check.That(outcome.File).IsNull();
        Check.That(outcome.Candidates).HasSize(2);
        Check.That(outcome.Candidates[0]).IsEqualTo("AnySubjectEntry in AnySubject.Entry.cs — the entry point of Subject");
        Check.That(outcome.Candidates[1]).Contains("Shop.Domain.AnySubjectEntry").And.Contains("already declared");
    }

    // The clash is on the declaration, so it is the namespace the entry point lands in that counts: the same
    // name elsewhere is a different type.
    [Fact(DisplayName = "The same name in another namespace is no clash.")]
    public void TheSameNameInAnotherNamespaceIsNoClash() {
        ScaffoldOutcome outcome = Subject.ScaffoldByName("Subject",
                                                         OnAny,
                                                         "public sealed class Subject { public Subject(int one) { } }",
                                                         "namespace Shop.Legacy { public sealed class AnySubjectEntry { } }");

        Check.That(outcome.Succeeded).IsTrue();
    }

    // And the remedy the refusal names first works for the same reason: the entry point moved to a namespace
    // of its own declares nothing where the name is borne.
    [Fact(DisplayName = "An entry point moved to its own namespace declares nothing where the name is borne.")]
    public void AnEntryPointMovedToItsOwnNamespaceDoesNotClash() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject { public Subject(int one) { } }

                                                   public sealed class AnySubjectEntry { }
                                                   """,
                                                   ScaffoldOptions.Default.WithEntryPoint(EntryPointOptions.OnAny.InNamespace("Shop.Tests.Entries")));

        Check.That(outcome.Succeeded).IsTrue();
        Check.That(outcome.EntryPoint!.Namespace).IsEqualTo("Shop.Tests.Entries");
    }

    // Arity is part of a type's identity: AnySubjectEntry<T> and a scaffolded AnySubjectEntry coexist, exactly
    // as the shadowing check of §7 reads the library's generic names.
    [Fact(DisplayName = "A generic type of the same name does not bear it.")]
    public void AGenericTypeOfTheSameNameDoesNotBearIt() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject { public Subject(int one) { } }

                                                   public sealed class AnySubjectEntry<T> { }
                                                   """,
                                                   OnAny);

        Check.That(outcome.Succeeded).IsTrue();
    }

    /// <summary>
    ///     A static root already declared is the part an earlier scaffold wrote (§4.5), not a clash.
    /// </summary>
    /// <remarks>
    ///     This is the one case where a name already borne is the design, and the check has to know it: a
    ///     check that compared every entry-point declaration would refuse the second type of every
    ///     <c>static:&lt;Name&gt;</c> project.
    /// </remarks>
    [Fact(DisplayName = "A static root already declared is the part an earlier scaffold wrote.")]
    public void AStaticRootAlreadyDeclaredIsAnEarlierPart() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject { public Subject(int one) { } }

                                                   public static partial class Arbitrary { }
                                                   """,
                                                   ScaffoldOptions.Default.WithEntryPoint(EntryPointOptions.OnStaticRoot("Arbitrary")));

        Check.That(outcome.Succeeded).IsTrue();
    }

    /// <summary>
    ///     A type a referenced project declares counts when the project can see it — a generators project
    ///     referenced by the tests, say — and is placed by its assembly, since it has no file here.
    /// </summary>
    /// <remarks>
    ///     The compiler would not refuse the declaration: <c>CS0436</c> is a warning, and the new type hides
    ///     the imported one. Every <c>new AnySubjectEntry()</c> in the project then binds to a static class,
    ///     which is the build the check exists to prevent.
    /// </remarks>
    [Fact(DisplayName = "An accessible type of a referenced assembly bears the name, placed by its assembly.")]
    public void AnAccessibleReferencedTypeBearsTheName() {
        ScaffoldOutcome outcome = Subject.ScaffoldByNameReferencing("namespace Shop.Domain { public sealed class AnySubjectEntry { } }",
                                                                    "Subject",
                                                                    OnAny,
                                                                    "public sealed class Subject { public Subject(int one) { } }");

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.EntryPointNameTaken);
        Check.That(outcome.Candidates[1]).IsEqualTo("Shop.Domain.AnySubjectEntry in Referenced — already declared");
    }

    [Fact(DisplayName = "An internal type of a referenced assembly is invisible here, and bears nothing.")]
    public void AnInternalReferencedTypeBearsNothing() {
        ScaffoldOutcome outcome = Subject.ScaffoldByNameReferencing("namespace Shop.Domain { internal sealed class AnySubjectEntry { } }",
                                                                    "Subject",
                                                                    OnAny,
                                                                    "public sealed class Subject { public Subject(int one) { } }");

        Check.That(outcome.Succeeded).IsTrue();
    }

    [Fact(DisplayName = "Across a run, the entry point naming another type's generator is refused, the other kept.")]
    public void AcrossARunTheEntryPointNamingAnotherGeneratorIsRefused() {
        ScaffoldOutcome voiceEntry = Subject.ScaffoldByName("VoiceEntry", OnAny, Voices);
        ScaffoldOutcome voice      = Subject.ScaffoldByName("Voice", OnAny, Voices);

        IReadOnlyList<ScaffoldOutcome> checkedRun = EntryPointNameCheck.AgainstTheBatch([voiceEntry, voice]);

        Check.That(checkedRun[0]).IsSameReferenceAs(voiceEntry);
        Check.That(checkedRun[1].Status).IsEqualTo(ScaffoldStatus.EntryPointNameTaken);
        Check.That(checkedRun[1].Candidates[0]).IsEqualTo("AnyVoiceEntry in AnyVoice.Entry.cs — the entry point of Voice");
        Check.That(checkedRun[1].Candidates[1]).IsEqualTo("AnyVoiceEntry in AnyVoiceEntry.cs — the generator of VoiceEntry, scaffolded in this run");
    }

    // The clash is between two names, not between two positions: whichever was asked for first, the same
    // scaffold is the one refused.
    [Fact(DisplayName = "The order the types were asked for in does not change which one is refused.")]
    public void TheOrderDoesNotChangeWhichOneIsRefused() {
        ScaffoldOutcome voiceEntry = Subject.ScaffoldByName("VoiceEntry", OnAny, Voices);
        ScaffoldOutcome voice      = Subject.ScaffoldByName("Voice", OnAny, Voices);

        IReadOnlyList<ScaffoldOutcome> checkedRun = EntryPointNameCheck.AgainstTheBatch([voice, voiceEntry]);

        Check.That(checkedRun[0].Status).IsEqualTo(ScaffoldStatus.EntryPointNameTaken);
        Check.That(checkedRun[1]).IsSameReferenceAs(voiceEntry);
    }

    [Fact(DisplayName = "Without an entry point there is nothing to declare, and the run passes through.")]
    public void WithoutAnEntryPointTheRunPassesThrough() {
        ScaffoldOutcome voiceEntry = Subject.ScaffoldByName("VoiceEntry", ScaffoldOptions.Default, Voices);
        ScaffoldOutcome voice      = Subject.ScaffoldByName("Voice", ScaffoldOptions.Default, Voices);

        IReadOnlyList<ScaffoldOutcome> checkedRun = EntryPointNameCheck.AgainstTheBatch([voiceEntry, voice]);

        Check.That(checkedRun[0]).IsSameReferenceAs(voiceEntry);
        Check.That(checkedRun[1]).IsSameReferenceAs(voice);
    }

    // A generator that was refused is not written, so nothing of it can be clashed with — and a refusal
    // carries no plan to compare in the first place.
    [Fact(DisplayName = "A refused scaffold bears no name, and is passed through as it was.")]
    public void ARefusedScaffoldBearsNoName() {
        ScaffoldOutcome refused = ScaffoldOutcome.Refused(ScaffoldStatus.TypeNotFound);
        ScaffoldOutcome voice   = Subject.ScaffoldByName("Voice", OnAny, Voices);

        IReadOnlyList<ScaffoldOutcome> checkedRun = EntryPointNameCheck.AgainstTheBatch([refused, voice]);

        Check.That(checkedRun[0]).IsSameReferenceAs(refused);
        Check.That(checkedRun[1]).IsSameReferenceAs(voice);
    }

    [Fact(DisplayName = "A run, and its every outcome, are required.")]
    public void ARunAndItsOutcomesAreRequired() {
        Check.ThatCode(() => EntryPointNameCheck.AgainstTheBatch(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => EntryPointNameCheck.AgainstTheBatch([null!])).Throws<ArgumentException>();
        Check.ThatCode(() => EntryPointNameCheck.AgainstTheCompilation(null!, ScaffoldOutcome.Refused(ScaffoldStatus.TypeNotFound)))
             .Throws<ArgumentNullException>();
    }

}
