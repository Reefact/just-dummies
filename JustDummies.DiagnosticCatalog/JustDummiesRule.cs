// The catalogue of JustDummies' own diagnostics: every JDxxx rule as compile-checked constants, so a
// consumer's [SuppressMessage] can name a rule the compiler resolves instead of a string nobody verifies
// (decision: ADR-0052).
//
// NOT generated at build time, and not a mirror. JustDummies.Analyzers reads its DiagnosticDescriptor
// arguments FROM these constants, which is the loop only a first-party catalogue can close: the rule the
// analyzer reports and the rule a consumer silences are then the same value by construction rather than two
// transcriptions of one string. Adding a rule starts here.

using DiagnosticCatalog;

namespace JustDummies.Diagnostics;

/// <summary>
///     The categories JustDummies groups its diagnostics under, declared once each.
/// </summary>
/// <remarks>
///     Reached only through the rule that carries it — write <c>JustDummiesRule.JD001.Category</c>, never the
///     category constant directly. The two spellings fold to the same string today and stop agreeing the day a
///     rule moves category: the rule member follows, a category named on its own does not, and the suppression
///     is left asserting a category the rule no longer carries.
/// </remarks>
[DiagnosticCategory]
public static class JustDummiesCategory {

    /// <summary>Rules about a run being replayable from the seed it reported.</summary>
    public const string Reproducibility = "JustDummies.Reproducibility";

    /// <summary>Rules about the recipe-versus-value distinction the library teaches: a generator is an immutable
    ///     recipe, and <c>Generate()</c> is the only thing that materializes a value from it.</summary>
    public const string Usage = "JustDummies.Usage";

    /// <summary>Rules that front-load, to build time, the subset of the library's run-time constraint checks that is
    ///     decidable from compile-time constants.</summary>
    public const string Constraints = "JustDummies.Constraints";

    /// <summary>Rules about assembling generators into bigger ones — what they share is that nothing goes wrong: the
    ///     composed generator builds, draws and returns a value. It is simply not the value the call site
    ///     describes.</summary>
    public const string Composition = "JustDummies.Composition";

}

/// <summary>
///     Every JustDummies diagnostic, as a rule a suppression can name.
/// </summary>
/// <example>
///     <code>
///     [SuppressMessage(JustDummiesRule.JD006.Category, JustDummiesRule.JD006.Id,
///                      Justification = "The drawn value is the subject of the assertion below.")]
///     </code>
/// </example>
/// <remarks>
///     A rule is never removed and a member is never renamed. These are <c>const</c>, so they are inlined into a
///     consumer's assembly at THEIR compile time: deleting one does not deprecate it, it breaks their build with
///     a message that names nothing they wrote.
/// </remarks>
public static class JustDummiesRule {

    /// <summary>
    ///     Where the per-rule documentation lives. Private and <c>const</c>: it composes into each rule's
    ///     <see cref="JD001.HelpLinkUri" /> at compile time, so the address is written once and every rule's link
    ///     follows it.
    /// </summary>
    private const string HelpLinkBase = "https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/analyzers/";

    /// <summary>The English page's extension, composed into every rule's link beside its identifier.</summary>
    private const string HelpLinkSuffix = ".en.md";

    /// <summary>An asynchronous body is passed to Any.Reproducibly</summary>
    [DiagnosticRule]
    public static class JD001 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD001);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Reproducibility;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "An asynchronous body is passed to Any.Reproducibly";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD001) + HelpLinkSuffix;

    }

    /// <summary>The task returned by Any.ReproduciblyAsync is discarded</summary>
    [DiagnosticRule]
    public static class JD002 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD002);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Reproducibility;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "The task returned by Any.ReproduciblyAsync is discarded";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD002) + HelpLinkSuffix;

    }

    /// <summary>An asynchronous body reaches Any.Reproducibly without being awaited</summary>
    [DiagnosticRule]
    public static class JD003 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD003);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Reproducibility;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "An asynchronous body reaches Any.Reproducibly without being awaited";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD003) + HelpLinkSuffix;

    }

    /// <summary>The result of a seeding call is discarded</summary>
    [DiagnosticRule]
    public static class JD004 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD004);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Reproducibility;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "The result of a seeding call is discarded";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD004) + HelpLinkSuffix;

    }

    /// <summary>A generator is rendered as text instead of the value it would draw</summary>
    [DiagnosticRule]
    public static class JD005 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD005);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Usage;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "A generator is rendered as text instead of the value it would draw";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD005) + HelpLinkSuffix;

    }

    /// <summary>The generator returned by a constraint is discarded</summary>
    [DiagnosticRule]
    public static class JD006 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD006);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Usage;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "The generator returned by a constraint is discarded";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD006) + HelpLinkSuffix;

    }

    /// <summary>An arbitrary value is drawn before [Reproducible] pins the seed</summary>
    [DiagnosticRule]
    public static class JD007 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD007);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Reproducibility;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "An arbitrary value is drawn before [Reproducible] pins the seed";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD007) + HelpLinkSuffix;

    }

    /// <summary>A theory's data provider draws an arbitrary value</summary>
    [DiagnosticRule]
    public static class JD008 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD008);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Reproducibility;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "A theory's data provider draws an arbitrary value";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD008) + HelpLinkSuffix;

    }

    /// <summary>An arbitrary value is drawn in a static initializer</summary>
    [DiagnosticRule]
    public static class JD009 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD009);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Reproducibility;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "An arbitrary value is drawn in a static initializer";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD009) + HelpLinkSuffix;

    }

    /// <summary>[Reproducible] is applied to a method that is not a test</summary>
    [DiagnosticRule]
    public static class JD010 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD010);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Reproducibility;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "[Reproducible] is applied to a method that is not a test";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD010) + HelpLinkSuffix;

    }

    /// <summary>A generator reaches a position that expected its value</summary>
    [DiagnosticRule]
    public static class JD011 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD011);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Usage;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "A generator reaches a position that expected its value";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD011) + HelpLinkSuffix;

    }

    /// <summary>A choice pool is built from generators rather than values</summary>
    [DiagnosticRule]
    public static class JD012 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD012);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Usage;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "A choice pool is built from generators rather than values";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD012) + HelpLinkSuffix;

    }

    /// <summary>A held collection is passed to Any.OneOf, making a pool of one</summary>
    [DiagnosticRule]
    public static class JD013 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD013);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Usage;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "A held collection is passed to Any.OneOf, making a pool of one";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD013) + HelpLinkSuffix;

    }

    /// <summary>A constant argument is one the generator rejects</summary>
    [DiagnosticRule]
    public static class JD014 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD014);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Constraints;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "A constant argument is one the generator rejects";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD014) + HelpLinkSuffix;

    }

    /// <summary>The declared string constraints admit no value</summary>
    [DiagnosticRule]
    public static class JD015 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD015);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Constraints;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "The declared string constraints admit no value";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD015) + HelpLinkSuffix;

    }

    /// <summary>The declared collection constraints admit no value</summary>
    [DiagnosticRule]
    public static class JD016 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD016);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Constraints;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "The declared collection constraints admit no value";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD016) + HelpLinkSuffix;

    }

    /// <summary>An enum constraint steps outside the generator's universe</summary>
    [DiagnosticRule]
    public static class JD017 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD017);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Constraints;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "An enum constraint steps outside the generator's universe";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD017) + HelpLinkSuffix;

    }

    /// <summary>A reproducibility scope is nested inside another</summary>
    [DiagnosticRule]
    public static class JD018 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD018);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Reproducibility;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "A reproducibility scope is nested inside another";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD018) + HelpLinkSuffix;

    }

    /// <summary>A replay seed is pinned in committed code</summary>
    [DiagnosticRule]
    public static class JD019 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD019);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Reproducibility;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "A replay seed is pinned in committed code";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD019) + HelpLinkSuffix;

    }

    /// <summary>An AnyContext is shared through a static field</summary>
    [DiagnosticRule]
    public static class JD020 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD020);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Reproducibility;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "An AnyContext is shared through a static field";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD020) + HelpLinkSuffix;

    }

    /// <summary>Any.UseSeed is given a blank replay snippet</summary>
    [DiagnosticRule]
    public static class JD021 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD021);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Reproducibility;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "Any.UseSeed is given a blank replay snippet";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD021) + HelpLinkSuffix;

    }

    /// <summary>A parallel work item draws without its own seed scope</summary>
    [DiagnosticRule]
    public static class JD022 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD022);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Reproducibility;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "A parallel work item draws without its own seed scope";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD022) + HelpLinkSuffix;

    }

    /// <summary>The declared scalar constraints admit no value</summary>
    [DiagnosticRule]
    public static class JD023 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD023);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Constraints;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "The declared scalar constraints admit no value";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD023) + HelpLinkSuffix;

    }

    /// <summary>A constraint narrows nothing</summary>
    [DiagnosticRule]
    public static class JD024 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD024);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Constraints;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "A constraint narrows nothing";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD024) + HelpLinkSuffix;

    }

    /// <summary>The same value is listed twice in a pool</summary>
    [DiagnosticRule]
    public static class JD025 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD025);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Constraints;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "The same value is listed twice in a pool";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD025) + HelpLinkSuffix;

    }

    /// <summary>The declared relative URI is empty</summary>
    [DiagnosticRule]
    public static class JD026 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD026);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Constraints;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "The declared relative URI is empty";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD026) + HelpLinkSuffix;

    }

    /// <summary>A Combine operand never reaches the composed value</summary>
    [DiagnosticRule]
    public static class JD027 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD027);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Composition;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "A Combine operand never reaches the composed value";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD027) + HelpLinkSuffix;

    }

    /// <summary>Distinctness is declared over an element type that has no value equality</summary>
    [DiagnosticRule]
    public static class JD028 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD028);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Composition;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "Distinctness is declared over an element type that has no value equality";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD028) + HelpLinkSuffix;

    }

    /// <summary>A value written into a pool that a declared constraint refuses, so no draw can ever yield it</summary>
    [DiagnosticRule]
    public static class JD029 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD029);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Constraints;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "A value written into a pool that a declared constraint refuses";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD029) + HelpLinkSuffix;

    }

    /// <summary>A string dummy that declares no length, so it draws the whole default spread</summary>
    [DiagnosticRule]
    public static class JD030 {

        /// <summary>The canonical identifier of this diagnostic.</summary>
        public const string Id = nameof(JD030);

        /// <summary>The category this diagnostic is grouped under.</summary>
        public const string Category = JustDummiesCategory.Constraints;

        /// <summary>The one-line summary the IDE shows beside the rule.</summary>
        public const string Title = "A string dummy that declares no length";

        /// <summary>The page explaining the condition this diagnostic detects.</summary>
        public const string HelpLinkUri = HelpLinkBase + nameof(JD030) + HelpLinkSuffix;

    }

}
