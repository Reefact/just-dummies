using System;
using System.Collections.Generic;
using System.Linq;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     Guarded domain types, driven through the whole engine and then held to what came out.
/// </summary>
/// <remarks>
///     The golden files record what the emitter writes for a plan; nothing recorded what the engine writes for
///     a <b>guard</b>. Every golden's parameters are unguarded or emptiness-guarded, so no approved file has
///     ever carried a bound pair, a count over an enum's members, a size above the library's cap or a sign
///     against an opposing bound — which is why a scaffold could raise the library's own rules, or refuse to
///     construct at all, with the whole suite green.
///     <para>
///         Each shape below is written as a developer writes it, and carries the outcome expected of it. A
///         shape marked with a defect is one the engine gets wrong today; the mark names which, and comes off
///         with the fix rather than with the test.
///     </para>
/// </remarks>
internal static class GuardCorpus {

    private const string Preamble = """
                                    using System;
                                    using System.Collections.Generic;

                                    namespace Shop.Domain;

                                    """;

    /// <summary>Every shape, by the name its test row carries.</summary>
    internal static IReadOnlyList<GuardedShape> All { get; } = [
        // ---- Sound: the engine already writes these correctly, and they keep the net honest. ----

        new GuardedShape("sound-positive", "Ticket", """
                                                     public sealed class Ticket {
                                                         public Ticket(int quantity) {
                                                             if (quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantity)); }
                                                         }
                                                     }
                                                     """),

        new GuardedShape("sound-non-empty", "Label", """
                                                     public sealed class Label {
                                                         public Label(string text) {
                                                             if (string.IsNullOrWhiteSpace(text)) { throw new ArgumentException(nameof(text)); }
                                                         }
                                                     }
                                                     """),

        new GuardedShape("sound-min-count", "Batch", """
                                                     public sealed class Batch {
                                                         public Batch(IReadOnlyList<string> lines) {
                                                             if (lines.Count < 1) { throw new ArgumentException(nameof(lines)); }
                                                         }
                                                     }
                                                     """),

        new GuardedShape("sound-bounded-below", "Quota", """
                                                         public sealed class Quota {
                                                             public Quota(int value) {
                                                                 if (value < 10) { throw new ArgumentOutOfRangeException(nameof(value)); }
                                                             }
                                                         }
                                                         """),

        // ---- D16: both bounds of a range declared separately, in all three families. ----

        new GuardedShape("range-length", "Reference", """
                                                      public sealed class Reference {
                                                          public Reference(string value) {
                                                              if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException(nameof(value)); }
                                                              if (value.Length < 8) { throw new ArgumentException(nameof(value)); }
                                                              if (value.Length > 20) { throw new ArgumentException(nameof(value)); }
                                                          }
                                                      }
                                                      """),

        new GuardedShape("range-numeric", "Score", """
                                                   public sealed class Score {
                                                       public Score(int value) {
                                                           if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); }
                                                           if (value > 100) { throw new ArgumentOutOfRangeException(nameof(value)); }
                                                       }
                                                   }
                                                   """),

        new GuardedShape("range-count", "Page", """
                                                public sealed class Page {
                                                    public Page(IReadOnlyList<string> lines) {
                                                        if (lines.Count < 2) { throw new ArgumentException(nameof(lines)); }
                                                        if (lines.Count > 5) { throw new ArgumentException(nameof(lines)); }
                                                    }
                                                }
                                                """),

        // ---- D5: an exact size beside a bound that excludes it. Bound.Exact is invisible to Contradicts. ----

        new GuardedShape("exact-versus-floor-count", "Mix", """
                                                            public sealed class Mix {
                                                                public Mix(IReadOnlyList<int> parts) {
                                                                    if (parts.Count != 2) { throw new ArgumentException(nameof(parts)); }
                                                                    if (parts.Count < 5) { throw new ArgumentException(nameof(parts)); }
                                                                }
                                                            }
                                                            """, beyondTheEngine: true),

        new GuardedShape("exact-versus-floor-length", "Code", """
                                                              public sealed class Code {
                                                                  public Code(string value) {
                                                                      if (value.Length < 10) { throw new ArgumentException(nameof(value)); }
                                                                      if (value.Length != 8) { throw new ArgumentException(nameof(value)); }
                                                                  }
                                                              }
                                                              """, beyondTheEngine: true),

        // ---- D6: the seeded NonEmpty against a size the guard pins at zero. The engine's own contradiction. ----

        new GuardedShape("blank-only", "Blank", """
                                                public sealed class Blank {
                                                    public Blank(string value) {
                                                        if (value.Length > 0) { throw new ArgumentException(nameof(value)); }
                                                    }
                                                }
                                                """),

        // ---- D7: a sign constraint beside an opposing bound. Bound.Sign is invisible to Contradicts. ----

        new GuardedShape("sign-versus-ceiling", "Offset", """
                                                          public sealed class Offset {
                                                              public Offset(int value) {
                                                                  if (value <= 0) { throw new ArgumentOutOfRangeException(nameof(value)); }
                                                                  if (value > -5) { throw new ArgumentOutOfRangeException(nameof(value)); }
                                                              }
                                                          }
                                                          """, beyondTheEngine: true),

        // ---- D8: a count floor on a set, written without consulting what the element row can draw. ----

        new GuardedShape("set-of-enum", "Role", """
                                                public enum Permission { Read, Write, Delete }

                                                public sealed class Role {
                                                    public Role(ISet<Permission> granted) {
                                                        if (granted.Count < 5) { throw new ArgumentException(nameof(granted)); }
                                                    }
                                                }
                                                """,
                         defect: "D8", beyondTheEngine: true),

        // ---- D9: a size constant above the library's producible cap, on each side of the family. ----

        new GuardedShape("above-cap-floor", "Body", """
                                                    public sealed class Body {
                                                        public Body(string text) {
                                                            if (text.Length < 1500000) { throw new ArgumentException(nameof(text)); }
                                                        }
                                                    }
                                                    """,
                         defect: "D9", beyondTheEngine: true),

        new GuardedShape("above-cap-ceiling", "Payload", """
                                                         public sealed class Payload {
                                                             public Payload(string text) {
                                                                 if (text.Length > 1048576) { throw new ArgumentException(nameof(text)); }
                                                             }
                                                         }
                                                         """,
                         defect: "D9", beyondTheEngine: true),

        // ---- D4: a constraint the ADR-0059 filter drops, leaving the draw unnarrowed and saying nothing. ----

        new GuardedShape("enum-excluding-default", "Assignment", """
                                                                 public enum Slot { None, Morning, Evening }

                                                                 public sealed class Assignment {
                                                                     public Assignment(Slot slot) {
                                                                         if (slot == Slot.None) { throw new ArgumentOutOfRangeException(nameof(slot)); }
                                                                     }
                                                                 }
                                                                 """,
                         defect: "D4")
    ];

    /// <summary>The shape names, as the theory rows carry them.</summary>
    internal static IEnumerable<string> Names() {
        return All.Select(shape => shape.Name);
    }

    /// <summary>The shapes whose domain a generator of this library can satisfy.</summary>
    internal static IEnumerable<string> Satisfiable() {
        return All.Where(shape => !shape.BeyondTheEngine).Select(shape => shape.Name);
    }

    /// <summary>The shapes whose domain it cannot, where the contract is a clean refusal.</summary>
    internal static IEnumerable<string> BeyondTheEngine() {
        return All.Where(shape => shape.BeyondTheEngine).Select(shape => shape.Name);
    }

    /// <summary>The shape a row names.</summary>
    internal static GuardedShape Named(string name) {
        return All.FirstOrDefault(shape => shape.Name == name)
            ?? throw new ArgumentOutOfRangeException(nameof(name), name, "No corpus shape by that name.");
    }

    /// <summary>One domain type, and what the engine is expected to make of its guards.</summary>
    internal sealed class GuardedShape {

        internal GuardedShape(string name,
                              string target,
                              string declarations,
                              string? defect = null,
                              bool beyondTheEngine = false) {
            Name            = name;
            Target          = target;
            Domain          = Preamble + declarations;
            Defect          = defect;
            BeyondTheEngine = beyondTheEngine;
        }

        /// <summary>The row's name, which is what a failure names.</summary>
        internal string Name { get; }

        /// <summary>The type argument a developer would type after <c>dum generate</c>.</summary>
        internal string Target { get; }

        /// <summary>The whole source the engine reads, preamble included.</summary>
        internal string Domain { get; }

        /// <summary>The defect this shape reproduces today, or null when the engine gets it right.</summary>
        internal string? Defect { get; }

        /// <summary>
        ///     Whether the domain declares something no generator of this library can draw.
        /// </summary>
        /// <remarks>
        ///     A developer's own contradiction, a size past the producible cap, a set wanting more distinct
        ///     values than its element row holds. ADR-0046 has one answer for all three and it is not a
        ///     cleverer draw: emit a chain the library accepts, and say plainly that the domain was not
        ///     honoured. So the bar moves rather than lifting — the generator must still CONSTRUCT, must
        ///     still raise no rule, and the recap must carry the refusal; only the draw is off the table,
        ///     since the domain itself rejects every value there is.
        /// </remarks>
        internal bool BeyondTheEngine { get; }

        /// <inheritdoc />
        public override string ToString() {
            return Name;
        }

    }

}
