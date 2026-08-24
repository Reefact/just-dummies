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
                                                """, beyondTheEngine: true),

        // ---- D9: a size constant above the library's producible cap, on each side of the family. ----

        new GuardedShape("above-cap-floor", "Body", """
                                                    public sealed class Body {
                                                        public Body(string text) {
                                                            if (text.Length < 1500000) { throw new ArgumentException(nameof(text)); }
                                                        }
                                                    }
                                                    """, beyondTheEngine: true),

        new GuardedShape("above-cap-ceiling", "Payload", """
                                                         public sealed class Payload {
                                                             public Payload(string text) {
                                                                 if (text.Length > 1048576) { throw new ArgumentException(nameof(text)); }
                                                             }
                                                         }
                                                         """, beyondTheEngine: true),

        // ---- An enum exclusion guard reads as AnyEnum<T>.DifferentFrom, so this domain is fully satisfiable. ----

        new GuardedShape("enum-excluding-default", "Assignment", """
                                                                 public enum Slot { None, Morning, Evening }

                                                                 public sealed class Assignment {
                                                                     public Assignment(Slot slot) {
                                                                         if (slot == Slot.None) { throw new ArgumentOutOfRangeException(nameof(slot)); }
                                                                     }
                                                                 }
                                                                 """),

        // ---- ADR-0086: the assigned guard-library idiom, the documented spelling of Ardalis.GuardClauses.
        // ---- The first statement is an assignment to state, so before the record this constructor read as
        // ---- two parameters nobody had constrained, under a recap that showed no doubt anywhere. Both rows
        // ---- read now, against the REAL package: two hundred draws through the real Guard.Against.

        new GuardedShape("ardalis-assigned", "Customer", """
                                                         public sealed class Customer {

                                                             public string Name { get; }

                                                             public int Points { get; }

                                                             public Customer(string name, int points) {
                                                                 Name   = Guard.Against.NullOrEmpty(name);
                                                                 Points = Guard.Against.Negative(points);
                                                             }

                                                         }
                                                         """, usings: "using Ardalis.GuardClauses;"),

        // ---- ADR-0086's range row, pinned at the boundary: OutOfRange was MEASURED inclusive at both ends,
        // ---- and a range this tight draws its boundaries constantly — a mapping wrong about either end
        // ---- cannot survive the two hundred draws.

        new GuardedShape("ardalis-range-assigned", "Percentage", """
                                                                 public sealed class Percentage {

                                                                     public int Value { get; }

                                                                     public Percentage(int value) {
                                                                         Value = Guard.Against.OutOfRange(value, nameof(value), 10, 11);
                                                                     }

                                                                 }
                                                                 """, usings: "using Ardalis.GuardClauses;"),

        // ---- The Toolkit's IsInRange was MEASURED half-open — the floor admitted, the ceiling rejected —
        // ---- so this domain admits exactly one value, and a mapping that admitted the ceiling too would
        // ---- throw on roughly half the draws.

        new GuardedShape("toolkit-half-open", "Slot", """
                                                      public sealed class Slot {

                                                          private readonly int index;

                                                          public Slot(int index) {
                                                              Guard.IsInRange(index, 5, 6, nameof(index));
                                                              this.index = index;
                                                          }

                                                      }
                                                      """, usings: "using CommunityToolkit.Diagnostics;"),

        // ---- A recognised library's method the table has no measured row for: declared validation the
        // ---- engine cannot vouch for, so the parameter blocks (ADR-0083) with the base kept underneath —
        // ---- while the mappable guard beside it still reads, which is the amplifier fixed: one unreadable
        // ---- guard-assignment no longer hides every other parameter's guards.

        new GuardedShape("guard-library-unmapped", "Reference", """
                                                                public sealed class Reference {

                                                                    public string Value { get; }

                                                                    public int Weight { get; }

                                                                    public Reference(string value, int weight) {
                                                                        Value  = Guard.Against.InvalidFormat(value, nameof(value), "^[A-Z]{3}$");
                                                                        Weight = Guard.Against.Negative(weight);
                                                                    }

                                                                }
                                                                """, usings: "using Ardalis.GuardClauses;", requiresVerification: true),

        // ---- §5.1's second rule, driven through every oracle at once: no accessible constructor, so
        // ---- Generate() calls the factory — whose own guards §5.3 reads. The draw is the proof the wiring
        // ---- holds: two hundred values through Reference.Create, every one of them accepted.

        new GuardedShape("factory-constructed", "Reference", """
                                                             public sealed class Reference {

                                                                 private readonly string value;

                                                                 private Reference(string value) {
                                                                     this.value = value;
                                                                 }

                                                                 public static Reference Create(string value) {
                                                                     if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException(nameof(value)); }

                                                                     return new Reference(value);
                                                                 }

                                                             }
                                                             """),

        // ---- The bug report behind this reading path: validation delegated to a helper, no `if` at all for
        // ---- §5.3 to parse. Its own domain is satisfiable — a length of 8 to 20 is well within the library's
        // ---- reach — the engine just cannot see the guard, which is why it blocks compilation rather than
        // ---- drawing (ADR-0083) instead of being reported merely `BeyondTheEngine`.

        new GuardedShape("helper-delegated-length", "Reference", """
                                                                  public sealed class Reference {

                                                                      private readonly string value;

                                                                      public Reference(string value) {
                                                                          Validate(value);
                                                                          this.value = value;
                                                                      }

                                                                      private static void Validate(string candidate) {
                                                                          if (string.IsNullOrWhiteSpace(candidate)) { throw new ArgumentException(nameof(candidate)); }
                                                                          if (candidate.Length < 8) { throw new ArgumentException(nameof(candidate)); }
                                                                          if (candidate.Length > 20) { throw new ArgumentException(nameof(candidate)); }
                                                                      }

                                                                  }
                                                                  """, requiresVerification: true),

        // ---- The guard the engine reads correctly, under a condition deciding whether it runs at all. Its
        // ---- domain is every int a `strict: false` caller may pass, and reading the helper as a bound
        // ---- narrowed the draw to zero and above — silently, since every draw still compiled and still
        // ---- constructed. So the engine blocks compilation over the guard it cannot vouch for (ADR-0083)
        // ---- rather than drawing under a recap claiming the parameter inferred.

        new GuardedShape("conditioned-helper-guard", "Allowance", """
                                                                  public sealed class Allowance {

                                                                      private readonly int amount;

                                                                      public Allowance(bool strict, int amount) {
                                                                          if (strict) { ArgumentOutOfRangeException.ThrowIfNegative(amount); }

                                                                          this.amount = amount;
                                                                      }

                                                                  }
                                                                  """, requiresVerification: true),

        // ---- The one shape where the engine was confidently wrong rather than blind: it read the second
        // ---- guard correctly and attributed it to a value the constructor no longer holds. The domain is 0
        // ---- to 100, well within the library's reach — what the engine cannot see is which value the guard
        // ---- is about, so it blocks compilation over the one guard it CAN vouch for (ADR-0083) rather than
        // ---- drawing under a recap claiming the whole range was read.

        new GuardedShape("reassigned-then-guarded", "Discount", """
                                                                public sealed class Discount {

                                                                    private readonly int percent;

                                                                    public Discount(int percent) {
                                                                        if (percent < 0) { throw new ArgumentOutOfRangeException(nameof(percent)); }
                                                                        percent = 100 - percent;
                                                                        if (percent < 0) { throw new ArgumentOutOfRangeException(nameof(percent)); }
                                                                        this.percent = percent;
                                                                    }

                                                                }
                                                                """, requiresVerification: true),


        // ---- PROBES for the eleven silent misreads an adversarial audit reproduced against the running
        // ---- engine (2026-08-24), each shape's own draw disagreeing with what the recap claimed. Each row
        // ---- was added `defect:`-marked before its fix, per ADR-0085's field-report signature — the audit's
        // ---- own measurement is the report — and the mark came off with the fix rather than with the test.
        // ---- They are permanent now: what a row asserts is the answer its finding was closed with, so a
        // ---- regression turns the row red rather than quietly restoring the silence it was written against.

        // ---- Finding 1. `Jumps` is asked of top-level statements only (`unskipped &= !Jumps(...)`); a
        // ---- `return` that is a SIBLING of the guard, nested one level inside an always-running `lock`, is
        // ---- neither a top-level statement nor an ancestor of the guard, so neither placement question sees
        // ---- it. The phantom floor of 50 destroys the real ceiling of 10 read above it, and a real draw
        // ---- (`value: 24`) is accepted where the constructor's own ceiling would have refused it.

        new GuardedShape("sibling-jump-destroys-a-real-bound", "Bracket", """
                                                                             public sealed class Bracket {

                                                                                 public Bracket(bool lenient, int value) {
                                                                                     if (value > 10) { throw new ArgumentOutOfRangeException(nameof(value)); }

                                                                                     lock (this) {
                                                                                         if (lenient) { Kept = value; return; }

                                                                                         ArgumentOutOfRangeException.ThrowIfLessThan(value, 50);
                                                                                     }

                                                                                     Kept = value;
                                                                                 }

                                                                                 public int Kept { get; }
                                                                             }
                                                                             """, requiresVerification: true),


        // ---- Finding 2. `Guards.cs` breaks the scan on the first assignment to state
        // ---- (`AssignsState(statement, model) && !IsGuardAssignment(...)`) BEFORE `MarkIfItRejects` runs, so
        // ---- a `throw` carried inside that very assignment's own right side is never asked whether it
        // ---- rejects. `code` reads as plain `inferred`, over a domain that rejects every string under 8 or
        // ---- over 20 characters.

        new GuardedShape("throw-inside-state-assignment", "Coupon", """
                                                                       public sealed class Coupon {

                                                                           public Coupon(string code, int uses) {
                                                                               Code = code.Length switch {
                                                                                   < 8  => throw new ArgumentException("Too short.", nameof(code)),
                                                                                   > 20 => throw new ArgumentException("Too long.", nameof(code)),
                                                                                   _    => code
                                                                               };

                                                                               if (uses < 1) { throw new ArgumentOutOfRangeException(nameof(uses)); }

                                                                               Uses = uses;
                                                                           }

                                                                           public string Code { get; }

                                                                           public int Uses { get; }
                                                                       }
                                                                       """, requiresVerification: true),


        // ---- Finding 3. `MarkIfValidatedElsewhere` matches an `ExpressionStatementSyntax` whose expression is
        // ---- the call itself, or a simple assignment of one. `Policy?.Enforce(code)` is a
        // ---- `ConditionalAccessExpressionSyntax` — neither shape — so the statement is skipped by both the
        // ---- rejection check and the delegation mark. `code` reads as plain `inferred` over a domain that
        // ---- rejects every string under 8 characters.

        new GuardedShape("guard-behind-conditional-access", "Voucher", """
                                                                          public sealed class Voucher {

                                                                              private static readonly CodePolicy? Policy = new CodePolicy();

                                                                              public Voucher(string code) {
                                                                                  Policy?.Enforce(code);

                                                                                  Code = code;
                                                                              }

                                                                              public string Code { get; }
                                                                          }

                                                                          public sealed class CodePolicy {
                                                                              public void Enforce(string candidate) {
                                                                                  if (candidate.Length < 8) { throw new ArgumentException("Too short.", nameof(candidate)); }
                                                                              }
                                                                          }
                                                                          """, requiresVerification: true),


        // ---- Finding 4. `Guards.Read` walks `declaration.Body.Statements` only, so a `: this(...)`
        // ---- initializer never enters the reading loop; `ParameterWrites` sees it, but only through
        // ---- `Initializer()` and `HandedByReference`, both asking whether the parameter was WRITTEN, never
        // ---- whether the delegated constructor REJECTS it. The delegated `percent <= 0` guard is lost —
        // ---- only the outer `percent > 100` reads — over a domain that rejects zero and negative values too.

        new GuardedShape("guard-in-constructor-initializer", "Allocation", """
                                                                              public sealed class Allocation {

                                                                                  private readonly int percent;
                                                                                  private readonly int other;

                                                                                  private Allocation(int percent, int other, bool _) {
                                                                                      if (percent <= 0) { throw new ArgumentOutOfRangeException(nameof(percent)); }

                                                                                      this.percent = percent;
                                                                                      this.other   = other;
                                                                                  }

                                                                                  public Allocation(int percent, int other) : this(percent, other, true) {
                                                                                      if (percent > 100) { throw new ArgumentOutOfRangeException(nameof(percent)); }
                                                                                  }
                                                                              }
                                                                              """),


        // ---- Finding 5. `GeneratorFor.Composed` calls `Guards.Read(factory, ...)` — the factory's own body
        // ---- only. `return new Coupon(number);` is a `ReturnStatementSyntax`; `MarkIfValidatedElsewhere`
        // ---- iterates `ExpressionStatementSyntax` only, so it never sees it, and `MarkIfItRejects` finds no
        // ---- `throw` in the factory either. The private constructor's `number <= 0` guard is read nowhere,
        // ---- with nothing marking the loss, over a composed parameter the domain still rejects at zero.

        new GuardedShape("factory-composed-over-guarded-ctor", "Holder", """
                                                                            public sealed class Coupon {

                                                                                private readonly int number;

                                                                                private Coupon(int number) {
                                                                                    if (number <= 0) { throw new ArgumentOutOfRangeException(nameof(number)); }

                                                                                    this.number = number;
                                                                                }

                                                                                public static Coupon Create(int number) { return new Coupon(number); }
                                                                            }

                                                                            public sealed class Holder {
                                                                                public Holder(Coupon coupon) { }
                                                                            }
                                                                            """),


        // ---- Finding 6a. ADR-0086's carve-out for a returning guard-library helper reaches
        // ---- `field = call;` only. `return new Rating(Guard.Against.OutOfRange(...));` hands the same,
        // ---- recognised call to a `ReturnStatementSyntax`, which `MarkIfValidatedElsewhere` does not scan —
        // ---- `stars` reads as plain `inferred`, with no bound at all, over a domain confined to 1 through 5.

        new GuardedShape("guard-library-return-position", "Rating", """
                                                                       public sealed class Rating {

                                                                           private Rating(int stars) { Stars = stars; }

                                                                           public int Stars { get; }

                                                                           public static Rating Create(int stars) {
                                                                               return new Rating(Guard.Against.OutOfRange(stars, nameof(stars), 1, 5));
                                                                           }
                                                                       }
                                                                       """, usings: "using Ardalis.GuardClauses;", requiresVerification: true),


        // ---- Finding 6b. The same carve-out reaches `field = call;` only; `decimal net =
        // ---- Guard.Against.NegativeOrZero(total);` is a `LocalDeclarationStatementSyntax`, a third shape it
        // ---- does not scan. `total` reads as plain `inferred` over a domain that rejects zero and negative
        // ---- amounts.

        new GuardedShape("guard-library-local-declaration", "Invoice", """
                                                                          public sealed class Invoice {

                                                                              public decimal Total { get; }

                                                                              public Invoice(decimal total) {
                                                                                  decimal net = Guard.Against.NegativeOrZero(total);
                                                                                  Total = net;
                                                                              }
                                                                          }
                                                                          """, usings: "using Ardalis.GuardClauses;", requiresVerification: true),


        // ---- Finding 7a. `LibraryGuards` folded `NullOrWhiteSpace` onto the same `.NonEmpty()` row as
        // ---- `NullOrEmpty` — a floor of one character, not a rejection of whitespace. The premise the fold
        // ---- rested on (an unconstrained `Any.String()` draws only ASCII letters and digits) was falsified by
        // ---- ADR-0075/0076 and never revisited; a short ceiling like this one's four-character cap makes an
        // ---- all-whitespace draw likely rather than rare. The row reads as `.NotBlank()` since ADR-0088,
        // ---- so the shape holds outright rather than being marked.

        new GuardedShape("guard-library-whitespace-ardalis", "CouponCode", """
                                                                              public sealed class CouponCode {

                                                                                  public CouponCode(string value) {
                                                                                      Guard.Against.NullOrWhiteSpace(value);
                                                                                      Guard.Against.StringTooLong(value, 4);
                                                                                      Value = value;
                                                                                  }

                                                                                  public string Value { get; }
                                                                              }
                                                                              """, usings: "using Ardalis.GuardClauses;"),


        // ---- Finding 7b. The same fold, CommunityToolkit's spelling: `IsNotNullOrWhiteSpace` also read as
        // ---- `.NonEmpty()`, which does not reject an all-whitespace draw under this domain's four-character
        // ---- ceiling. Closed by the same member.

        new GuardedShape("guard-library-whitespace-toolkit", "Ticker", """
                                                                          public sealed class Ticker {

                                                                              private readonly string symbol;

                                                                              public Ticker(string symbol) {
                                                                                  Guard.IsNotNullOrWhiteSpace(symbol, nameof(symbol));
                                                                                  if (symbol.Length > 4) { throw new ArgumentException("too long", nameof(symbol)); }

                                                                                  this.symbol = symbol;
                                                                              }
                                                                          }
                                                                          """, usings: "using CommunityToolkit.Diagnostics;"),

        // ---- Finding 12. The same fold as 7a/7b, in the two spellings the first fix did not touch: `Guards`
        // ---- kept `IsNullOrWhiteSpace` in the same table as `IsNullOrEmpty`, and `ThrowIfNullOrWhiteSpace` in
        // ---- the same table as `ThrowIfNullOrEmpty`, so both reached `Emptiness()` and read as `.NonEmpty()`. Six corpus rows already spell the BCL
        // ---- check and pass, because none of them caps the length: at the default 1024-character spread an
        // ---- all-whitespace draw is astronomically unlikely, and it is a short ceiling that makes it common.

        new GuardedShape("guard-bcl-whitespace-condition", "Slug", """
                                                                      public sealed class Slug {

                                                                          public Slug(string value) {
                                                                              if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException("blank", nameof(value)); }
                                                                              if (value.Length > 4) { throw new ArgumentException("too long", nameof(value)); }

                                                                              Value = value;
                                                                          }

                                                                          public string Value { get; }
                                                                      }
                                                                      """),


        new GuardedShape("guard-bcl-whitespace-throw-helper", "Handle", """
                                                                           public sealed class Handle {

                                                                               private readonly string name;

                                                                               public Handle(string name) {
                                                                                   ArgumentException.ThrowIfNullOrWhiteSpace(name);
                                                                                   if (name.Length > 4) { throw new ArgumentException("too long", nameof(name)); }

                                                                                   this.name = name;
                                                                               }
                                                                           }
                                                                           """),


        // ---- Finding 8. `Guards.IsSize` accepts `tags.Count` because the receiver IS the parameter, without
        // ---- asking whether the parameter's TYPE is one the size family means. The family is then chosen
        // ---- from the parameter's own type (not a collection), so `WithMinLength` lands on the factory's
        // ---- inner `Any.String()` — legal there, understood there, and a complete non sequitur about `Tags`,
        // ---- whose own domain rejects fewer than three comma-separated entries regardless of string length.

        new GuardedShape("composed-count-as-length", "Article", """
                                                                   public sealed class Tags {

                                                                       private Tags(string csv) { Csv = csv; }

                                                                       public static Tags Parse(string csv) {
                                                                           if (string.IsNullOrWhiteSpace(csv)) { throw new ArgumentException("blank", nameof(csv)); }

                                                                           return new Tags(csv);
                                                                       }

                                                                       public string Csv { get; }

                                                                       public int Count { get { return Csv.Split(',').Length; } }
                                                                   }

                                                                   public sealed class Article {

                                                                       public Article(Tags tags) {
                                                                           if (tags.Count < 3) { throw new ArgumentException("three tags", nameof(tags)); }
                                                                           Tags = tags;
                                                                       }

                                                                       public Tags Tags { get; }
                                                                   }
                                                                   """, requiresVerification: true),


        // ---- Finding 10. §5.1's target-path rule reads the chosen `Create` factory's own body
        // ---- (`if (string.IsNullOrWhiteSpace(value))`) and stops there; the private constructor `Create`
        // ---- delegates to, guarding `value.Length < 8`, is never read and nothing marks the loss. `value`
        // ---- reads as `Any.String().NonEmpty()`, provenance `Guard`, over a domain that also rejects every
        // ---- string under 8 characters.

        new GuardedShape("factory-target-over-guarded-ctor", "Reference", """
                                                                             public sealed class Reference {

                                                                                 private readonly string value;

                                                                                 private Reference(string value) {
                                                                                     if (value.Length < 8) { throw new ArgumentException("too short", nameof(value)); }
                                                                                     this.value = value;
                                                                                 }

                                                                                 public static Reference Create(string value) {
                                                                                     if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException("blank", nameof(value)); }

                                                                                     return new Reference(value);
                                                                                 }
                                                                             }
                                                                             """),


        // ---- Finding 11. Read correctly, about the wrong value — `Guards.cs`'s own remarks name this class
        // ---- and mark only the ADR-0083 instance. `tags.Count` is read and its family chosen from `Tags`
        // ---- (not a collection, so the length family), but `GeneratorFor.Chain` renders the constraint onto
        // ---- `Any.String()`, the factory's SOURCE generator, before the `.As(Tags.Of)` hop — a value the
        // ---- generator no longer draws once composed. The recap reports `guard` with full confidence.

        new GuardedShape("composed-value-attributed-to-source-generator", "Bundle", """
                                                                                       public sealed class Tags {

                                                                                           private readonly IReadOnlyList<string> items;

                                                                                           private Tags(IReadOnlyList<string> items) { this.items = items; }

                                                                                           public static Tags Of(string csv) { return new Tags(csv.Split(',')); }

                                                                                           public int Count { get { return items.Count; } }
                                                                                       }

                                                                                       public sealed class Bundle {

                                                                                           private readonly Tags tags;

                                                                                           public Bundle(Tags tags) {
                                                                                               if (tags.Count < 3) { throw new ArgumentException("at least three tags", nameof(tags)); }
                                                                                               this.tags = tags;
                                                                                           }
                                                                                       }
                                                                                       """, requiresVerification: true),


        // ---- AUDIT (lens 1). The delegated-guard fold this branch adds carries the delegated reading's
        // ---- CONSTRAINTS across the hop and leaves its DOUBT behind. `Reference(string)` delegates to a
        // ---- private constructor whose two guards are one the engine reads (`value.Length < 8`) and one it
        // ---- cannot (`StartsWith`). Read directly the same body earns `UnreadGuards` and a sentinel; read
        // ---- through `: this(value, false)` it earns `Guard`, requiresVerification=False, a clean compile
        // ---- and a first-draw rejection.

        new GuardedShape("delegated-ctor-drops-the-unread-mark", "Reference", """
                                                                             public sealed class Reference {

                                                                                 private readonly string value;
                                                                                 private readonly bool trusted;

                                                                                 private Reference(string value, bool trusted) {
                                                                                     if (!value.StartsWith("REF-", StringComparison.Ordinal)) { throw new ArgumentException("prefix", nameof(value)); }
                                                                                     if (value.Length < 8) { throw new ArgumentException("short", nameof(value)); }
                                                                                     this.value = value;
                                                                                     this.trusted = trusted;
                                                                                 }

                                                                                 public Reference(string value) : this(value, false) { }
                                                                             }
                                                                             """, defect: "the delegated fold carries constraints across the hop and leaves the doubt behind"),


        // ---- AUDIT (lens 1). The same loss on the factory half of the fold: `Create` returns
        // ---- `new Token(value)`, the constructed constructor's readable guard is folded onto `value`, and
        // ---- the unread `StartsWith` guard beside it is dropped without a mark.

        new GuardedShape("factory-returned-ctor-drops-the-unread-mark", "Token", """
                                                                                public sealed class Token {

                                                                                    private readonly string value;

                                                                                    private Token(string value) {
                                                                                        if (!value.StartsWith("T-", StringComparison.Ordinal)) { throw new ArgumentException("prefix", nameof(value)); }
                                                                                        if (value.Length < 6) { throw new ArgumentException("short", nameof(value)); }
                                                                                        this.value = value;
                                                                                    }

                                                                                    public static Token Create(string value) {
                                                                                        return new Token(value);
                                                                                    }
                                                                                }
                                                                                """, defect: "the same doubt is lost on the factory half of the fold"),


        // ---- AUDIT (lens 1). `HandedTo` maps an argument to `delegatedTo.Parameters[index]` without asking
        // ---- whether the call is in EXPANDED form. `new Blocks(group)` fills one ELEMENT of `params
        // ---- IReadOnlyList<string>[] groups`, so the guard `groups.Length < 4` — four groups — is folded onto
        // ---- `group` as a count of four ITEMS. The member resolves on a list generator, so nothing drops and
        // ---- nothing is marked: a guard read correctly, about a value the generator does not draw.

        new GuardedShape("params-expanded-fold-attributes-the-arrays-count", "Blocks", """
                                                                                       public sealed class Blocks {

                                                                                           private readonly IReadOnlyList<string> first;

                                                                                           private Blocks(params IReadOnlyList<string>[] groups) {
                                                                                               if (groups.Length < 4) { throw new ArgumentException("four groups", nameof(groups)); }
                                                                                               this.first = groups[0];
                                                                                           }

                                                                                           public static Blocks Of(IReadOnlyList<string> group) {
                                                                                               return new Blocks(group);
                                                                                           }
                                                                                       }
                                                                                       """, defect: "the fold maps positionally through an expanded params call"),


        // ---- AUDIT (lens 1). The fold's own skip path is silent. `value = value.Trim();` before the return
        // ---- makes `writes.Precede` decline the fold — correctly, the constructed type guards the trimmed
        // ---- value — but nothing marks the decline, so `value` reports as `inferred`, requiresVerification
        // ---- False, over a constructor that rejects every draw the trim shortens below eight.

        new GuardedShape("factory-that-rewrites-before-handoff-says-nothing", "Trimmed", """
                                                                                         public sealed class Trimmed {

                                                                                             private readonly string value;

                                                                                             private Trimmed(string value) {
                                                                                                 if (value.Length < 8) { throw new ArgumentException("short", nameof(value)); }
                                                                                                 this.value = value;
                                                                                             }

                                                                                             public static Trimmed Create(string value) {
                                                                                                 value = value.Trim();

                                                                                                 return new Trimmed(value);
                                                                                             }
                                                                                         }
                                                                                         """, defect: "the fold declines a rewritten parameter without marking the decline")

    ];

    /// <summary>The shape names, as the theory rows carry them.</summary>
    internal static IEnumerable<string> Names() {
        return All.Select(shape => shape.Name);
    }

    /// <summary>
    ///     The shapes whose domain a generator of this library can satisfy, and whose chain the engine vouches
    ///     for — so the emitted file is expected to compile as written.
    /// </summary>
    internal static IEnumerable<string> SatisfiableNames() {
        return All.Where(shape => !shape.BeyondTheEngine && !shape.RequiresVerification).Select(shape => shape.Name);
    }

    /// <summary>The shapes whose domain it cannot, where the contract is a clean refusal.</summary>
    internal static IEnumerable<string> BeyondTheEngineNames() {
        return All.Where(shape => shape.BeyondTheEngine).Select(shape => shape.Name);
    }

    /// <summary>
    ///     The shapes whose domain the engine COULD satisfy, but whose guard it could not vouch for — so the
    ///     emitted file is expected to block compilation, with the recipe it did infer kept underneath (§5.6).
    /// </summary>
    internal static IEnumerable<string> RequiresVerificationNames() {
        return All.Where(shape => shape.RequiresVerification).Select(shape => shape.Name);
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
                              bool beyondTheEngine = false,
                              bool requiresVerification = false,
                              string? usings = null) {
            Name                 = name;
            Target               = target;
            Domain               = Preambled(usings) + declarations;
            Defect               = defect;
            BeyondTheEngine      = beyondTheEngine;
            RequiresVerification = requiresVerification;
        }

        /// <summary>The preamble, with the one extra using a guard-library shape opens (ADR-0086).</summary>
        private static string Preambled(string? usings) {
            return usings is null
                       ? Preamble
                       : Preamble.Replace("namespace Shop.Domain;", usings + "\n\nnamespace Shop.Domain;");
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
        ///     values than its element row holds, an invariant the closed set of §5.3 has no member to say.
        ///     ADR-0046 has one answer for all four and it is not a
        ///     cleverer draw: emit a chain the library accepts, and say plainly that the domain was not
        ///     honoured. So the bar moves rather than lifting — the generator must still CONSTRUCT, must
        ///     still raise no rule, and the recap must carry the refusal; only the draw is off the table,
        ///     since the domain itself rejects every value there is.
        /// </remarks>
        internal bool BeyondTheEngine { get; }

        /// <summary>
        ///     Whether the domain is satisfiable, but a guard toward it is one the engine could not vouch for
        ///     (§5.6) — so the emitted file is expected NOT to compile as written, unlike every other shape.
        /// </summary>
        /// <remarks>
        ///     Distinct from <see cref="BeyondTheEngine" />: there the domain itself admits nothing the library
        ///     can draw; here it admits plenty, the engine simply cannot see the rule that says so, and the
        ///     factory blocks compilation rather than risk a value the real constructor rejects (ADR-0083).
        /// </remarks>
        internal bool RequiresVerification { get; }

        /// <inheritdoc />
        public override string ToString() {
            return Name;
        }

    }

}
