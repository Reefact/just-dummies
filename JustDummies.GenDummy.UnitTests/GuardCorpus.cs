using System;
using System.Collections.Generic;
using System.Linq;

namespace JustDummies.GenDummy.UnitTests;

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
                                                     """, idioms: ["`p <= 0`; or `p < 1` on an **integral** type"]),

        new GuardedShape("sound-non-empty", "Label", """
                                                     public sealed class Label {
                                                         public Label(string text) {
                                                             if (string.IsNullOrWhiteSpace(text)) { throw new ArgumentException(nameof(text)); }
                                                         }
                                                     }
                                                     """, idioms: ["`string.IsNullOrWhiteSpace(p)`"]),

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
                                                         """, idioms: ["`p < N`"]),

        // ---- D16: both bounds of a range declared separately, in all three families. ----

        new GuardedShape("range-length", "Reference", """
                                                      public sealed class Reference {
                                                          public Reference(string value) {
                                                              if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException(nameof(value)); }
                                                              if (value.Length < 8) { throw new ArgumentException(nameof(value)); }
                                                              if (value.Length > 20) { throw new ArgumentException(nameof(value)); }
                                                          }
                                                      }
                                                      """, idioms: ["`p.Length > N`", "`p.Length < N`"]),

        new GuardedShape("range-numeric", "Score", """
                                                   public sealed class Score {
                                                       public Score(int value) {
                                                           if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); }
                                                           if (value > 100) { throw new ArgumentOutOfRangeException(nameof(value)); }
                                                       }
                                                   }
                                                   """, idioms: ["`p < 0`", "`p > N`"]),

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

        // ---- An enum exclusion guard reads as DummyEnum<T>.DifferentFrom, so this domain is fully satisfiable. ----

        new GuardedShape("enum-excluding-default", "Assignment", """
                                                                 public enum Slot { None, Morning, Evening }

                                                                 public sealed class Assignment {
                                                                     public Assignment(Slot slot) {
                                                                         if (slot == Slot.None) { throw new ArgumentOutOfRangeException(nameof(slot)); }
                                                                     }
                                                                 }
                                                                 """, idioms: ["`p == E.Member`"]),

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
                                                         """, usings: "using Ardalis.GuardClauses;", idioms: ["`Guard.Against.NullOrEmpty(p)` / `Guard.IsNotNullOrEmpty(p, …)`", "`Guard.Against.Negative(p)`"]),

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
                                                                 """, usings: "using Ardalis.GuardClauses;", idioms: ["`Guard.Against.OutOfRange(p, name, from, to)` — measured inclusive at **both** ends"]),

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
                                                      """, usings: "using CommunityToolkit.Diagnostics;", idioms: ["`Guard.IsInRange(p, min, max)` — measured **half-open**"]),

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


        // ---- Finding 5, re-scoped by ADR-0089. A composed parameter no longer routes through
        // ---- `GeneratorFor.Composed`'s factory-guard reading at all — `Holder(Coupon coupon)` now draws
        // ---- `new DummyCoupon()`, and `Coupon`'s own guards are `DummyCoupon`'s business, not this row's. The
        // ---- mechanism the original shape exercised — `MergeConstructedReturnGuards` reading a factory's
        // ---- `return new T(args)` back onto the factory's own parameter — still applies wherever `Coupon`
        // ---- is the scaffolded TARGET (§5.1's own factory path, `Scaffolder.ChosenFactory`), so this row now
        // ---- scaffolds `Coupon` directly rather than through a composing `Holder`. Finding 10 covers the
        // ---- same mechanism on a `string`; this keeps the `int` family distinct.

        new GuardedShape("factory-target-over-guarded-ctor-int", "Coupon", """
                                                                              public sealed class Coupon {

                                                                                  private readonly int number;

                                                                                  private Coupon(int number) {
                                                                                      if (number <= 0) { throw new ArgumentOutOfRangeException(nameof(number)); }

                                                                                      this.number = number;
                                                                                  }

                                                                                  public static Coupon Create(int number) { return new Coupon(number); }
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
        // ---- rested on (an unconstrained `Dummy.String()` draws only ASCII letters and digits) was falsified by
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
                                                                              """, usings: "using Ardalis.GuardClauses;", idioms: ["`Guard.Against.NullOrWhiteSpace(p)` / `Guard.IsNotNullOrWhiteSpace(p, …)`", "`Guard.Against.StringTooShort(p, min)`, `StringTooLong(p, max)`, `LengthOutOfRange(p, min, max)`"]),


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


        // ---- Finding 10. §5.1's target-path rule reads the chosen `Create` factory's own body
        // ---- (`if (string.IsNullOrWhiteSpace(value))`) and stops there; the private constructor `Create`
        // ---- delegates to, guarding `value.Length < 8`, is never read and nothing marks the loss. `value`
        // ---- reads as `Dummy.String().NonEmpty()`, provenance `Guard`, over a domain that also rejects every
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


        // ---- Findings 8 and 11, retired by ADR-0089. Both pinned a MISATTRIBUTION: a `.Count`/`.Length`
        // ---- guard about a composed value landing on the generator composition used to DERIVE that value's
        // ---- recipe (the factory's own inner string or list argument) rather than being dropped. Composition
        // ---- no longer derives a recipe for anything — a composed parameter is `new DummyTags()`, with no
        // ---- chain for a misattributed constraint to land on — so the failure mode these rows existed to
        // ---- catch is now structurally impossible, not merely fixed. What survives of the underlying
        // ---- question — does the `.Count` guard get marked `unread guards` rather than silently dropped? —
        // ---- is pinned directly, cheaper and without a same-suite `DummyTags` to compile against, by
        // ---- `GuardReadingTests.ACountReadOffANonCollectionNonStringParameterIsUnread`.

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
                                                                             """, requiresVerification: true),


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
                                                                                """, requiresVerification: true),


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
                                                                                       """, requiresVerification: true),


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
                                                                                         """, requiresVerification: true),


        // ---- AUDIT (second sweep). The fold speaks at three of its four declines, and carries one of the
        // ---- TWO doubt channels GuardReading holds. The fourth decline -- an argument that is not a bare
        // ---- identifier -- is a bare `continue`; and `SourceAvailable` never crosses the hop, so a
        // ---- constructor whose body the engine cannot see at all is indistinguishable from one read clean.

        new GuardedShape("factory-computed-argument-says-nothing", "Offset", """
                                                                                public sealed class Offset {

                                                                                    private readonly int value;

                                                                                    private Offset(int value) {
                                                                                        if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); }

                                                                                        this.value = value;
                                                                                    }

                                                                                    public static Offset Create(int value) {
                                                                                        return new Offset(value + 1);
                                                                                    }
                                                                                }
                                                                                """, requiresVerification: true),


        new GuardedShape("delegated-ctor-without-a-body-says-nothing", "Marker", """
                                                                                    public sealed class Marker {

                                                                                        private readonly int kept;

                                                                                        private Marker(int value) => kept = value < 0 ? throw new ArgumentOutOfRangeException(nameof(value)) : value;

                                                                                        public Marker(int value, bool _) : this(value) { }
                                                                                    }
                                                                                    """, requiresVerification: true),


        // ---- AUDIT (second sweep). `IsCall` inspects only the containing type and the method name, never the
        // ---- ARGUMENTS, so `string.IsNullOrEmpty(value.Trim())` reads as a guard about `value` itself. Every
        // ---- other row of the closed set keeps the subject-identity discipline -- `TryRecogniseThrowHelper`
        // ---- and `LibraryGuards.TryRead` both test it -- and both counterfactual spellings earn a mark.

        new GuardedShape("emptiness-check-on-a-derived-value", "Slugged", """
                                                                             public sealed class Slugged {

                                                                                 public Slugged(string value) {
                                                                                     if (string.IsNullOrEmpty(value.Trim())) { throw new ArgumentException("blank", nameof(value)); }
                                                                                     if (value.Length > 4) { throw new ArgumentException("long", nameof(value)); }

                                                                                     Value = value;
                                                                                 }

                                                                                 public string Value { get; }
                                                                             }
                                                                             """, requiresVerification: true)
,


        // ---- AUDIT (second sweep), GeneratorFor. Pre-existing on main rather than introduced here, and the
        // ---- same signature: compile clean, zero diagnostics at ANY severity, recap `guard`, and then the
        // ---- generator does not even construct. `DistinctElements` answers only for bool and enum, counts an
        // ---- enum's DECLARED members rather than its distinct values, and never unwraps a nullable; and the
        // ---- provenance of a composed element is dropped when the collection generator is rebuilt.

        new GuardedShape("set-of-char-count", "Palette", """
                                                            public sealed class Palette {

                                                                public Palette(ISet<char> glyphs) {
                                                                    if (glyphs.Count < 200) { throw new ArgumentException("too few", nameof(glyphs)); }

                                                                    Glyphs = glyphs;
                                                                }

                                                                public ISet<char> Glyphs { get; }
                                                            }
                                                            """, requiresVerification: true),


        new GuardedShape("set-of-nullable-enum-count", "Roster", """
                                                                    public enum Span { Morning, Noon, Night }

                                                                    public sealed class Roster {

                                                                        public Roster(ISet<Span?> spans) {
                                                                            if (spans.Count < 5) { throw new ArgumentException("too few", nameof(spans)); }

                                                                            Spans = spans;
                                                                        }

                                                                        public ISet<Span?> Spans { get; }
                                                                    }
                                                                    """, requiresVerification: true),


        new GuardedShape("set-of-aliased-enum-count", "Band", """
                                                                 public enum Grade { Low = 1, Medium = 2, High = 3, Min = 1, Max = 3 }

                                                                 public sealed class Band {

                                                                     public Band(ISet<Grade> grades) {
                                                                         if (grades.Count < 5) { throw new ArgumentException("too few", nameof(grades)); }

                                                                         Grades = grades;
                                                                     }

                                                                     public ISet<Grade> Grades { get; }
                                                                 }
                                                                 """, requiresVerification: true),


        // ---- AUDIT (mirror direction). The delegated-guard fold already strips a null-forgiving `!` before
        // ---- checking whether an argument IS the outer parameter (`Unsuppressed`) -- `!` is a compile-time
        // ---- annotation with no run-time effect, so `value!` and `value` are the same value everywhere this
        // ---- reads. The ordinary condition-parsing path never learned the same lesson: `string.IsNullOrEmpty
        // ---- (value!)` in a ctor's own body was declined by `IsParameter`, which stripped parentheses but not
        // ---- `!`, so the guard fell to `unread guards` -- not a lie, but a refusal of a shape the engine can
        // ---- and already does read one hop over.

        new GuardedShape("null-forgiving-blankness-read-directly", "Receipt", """
                                                                                public sealed class Receipt {

                                                                                    private readonly string value;

                                                                                    public Receipt(string value) {
                                                                                        if (string.IsNullOrEmpty(value!)) { throw new ArgumentException(nameof(value)); }

                                                                                        this.value = value;
                                                                                    }
                                                                                }
                                                                                """, idioms: ["`string.IsNullOrEmpty(p)`, `p.Length == 0`, `p.Length < 1`"]),



        // ---- AUDIT. The true refusal edge of every scalar element row was measured by walking WithMinCount(n)
        // ---- to the ceiling the engine still declares, and the mirror named only four of the six finite
        // ---- domains that measurement found: Int16 and UInt16 slip through, both reading unbounded, so a
        // ---- floor above 65 536 -- the whole of either type -- is written with confidence over a generator
        // ---- that cannot construct it.

        new GuardedShape("set-of-int16-count", "Batch", """
                                                        public sealed class Batch {

                                                            public Batch(ISet<short> codes) {
                                                                if (codes.Count < 70000) { throw new ArgumentException("too few", nameof(codes)); }
                                                            }
                                                        }
                                                        """, requiresVerification: true),

        // ---- AUDIT (mirror completeness). The row above closed Int16/UInt16 by hand. Enumerating the
        // ---- library's cardinality-bearing rows afterwards found one more the mirror never named: `Half`.
        // ---- Both sides answer "unbounded" for different reasons -- the engine because `Half` is not in
        // ---- its switch, the library because a floating-point range is a continuum its shared spec
        // ---- declines to count -- so a floor of 200 000 over `ISet<Half>` is declared with confidence
        // ---- over a type that holds 63 488 finite values at all.
        new GuardedShape("set-of-half-count", "Reading", """
                                                        public sealed class Reading {

                                                            public Reading(ISet<Half> samples) {
                                                                if (samples.Count < 200000) { throw new ArgumentException("too few", nameof(samples)); }
                                                            }
                                                        }
                                                        """, requiresVerification: true),

        // ---- Retired by ADR-0089, on the same footing as findings 8 and 11. The original shape pinned a
        // ---- composed ELEMENT's factory guard being read correctly and then losing its `unread guards` mark
        // ---- when the collection generator was rebuilt around it -- `Dummy.ListOf(...)` behind which the doubt
        // ---- about `Tag` disappeared. Composition no longer reads an element's factory guards at all: `Tag`
        // ---- draws as `new DummyTag()`, exactly like a top-level composed parameter, and its own guards are
        // ---- `DummyTag`'s business. Scaffolding `Sheet` without `DummyTag` in the compilation now fails with
        // ---- `CS0246: DummyTag could not be found` -- measured directly, not left to this row's assumption that
        // ---- a chain still gets built here to lose doubt about.

        // ---- Cells of the closed table of §5.3 that no shape had ever exercised. RecognisedIdiomCoverageTests
        // ---- found eleven of twenty-eight in that state -- not defects, but the state every defect of this
        // ---- campaign was found in. One shape per cell, each minimal, so a failure names the cell.

        new GuardedShape("bcl-null-check", "Recipient", """
                                                        public sealed class Recipient {
                                                            public Recipient(string address) {
                                                                if (address is null) { throw new ArgumentNullException(nameof(address)); }
                                                            }
                                                        }
                                                        """, idioms: ["`p is null`, `p == null`, or the assigned `f = p ?? throw new ArgumentNullException(nameof(p));`"]),

        // ---- The assigned spelling of the same null-check, fused into the write rather than standing before
        // ---- it (§5.3) -- and two parameters guarded that way in sequence, since the shape's whole point is
        // ---- that the second one is still read rather than silently skipped once the first ends up looking
        // ---- like an ordinary write to state.
        new GuardedShape("bcl-assigned-null-check", "Parcel", """
                                                              public sealed class Parcel {
                                                                  public Parcel(string sender, string recipient) {
                                                                      Sender    = sender ?? throw new ArgumentNullException(nameof(sender));
                                                                      Recipient = recipient ?? throw new ArgumentNullException(nameof(recipient));
                                                                  }

                                                                  public string Sender    { get; }
                                                                  public string Recipient { get; }
                                                              }
                                                              """, idioms: ["`p is null`, `p == null`, or the assigned `f = p ?? throw new ArgumentNullException(nameof(p));`"]),

        new GuardedShape("bcl-negative-only", "Drawdown", """
                                                          public sealed class Drawdown {
                                                              public Drawdown(int delta) {
                                                                  if (delta >= 0) { throw new ArgumentOutOfRangeException(nameof(delta)); }
                                                              }
                                                          }
                                                          """, idioms: ["`p >= 0`"]),

        new GuardedShape("bcl-non-zero", "Stride", """
                                                   public sealed class Stride {
                                                       public Stride(int step) {
                                                           if (step == 0) { throw new ArgumentOutOfRangeException(nameof(step)); }
                                                       }
                                                   }
                                                   """, idioms: ["`p == 0`"]),

        new GuardedShape("bcl-guid-non-empty", "Correlation", """
                                                              public sealed class Correlation {
                                                                  public Correlation(Guid id) {
                                                                      if (id == Guid.Empty) { throw new ArgumentException(nameof(id)); }
                                                                  }
                                                              }
                                                              """, idioms: ["`p == Guid.Empty`"]),

        new GuardedShape("bcl-enum-defined", "Assignment", """
                                                           public enum Shift { Morning, Evening }

                                                           public sealed class Assignment {
                                                               public Assignment(Shift shift) {
                                                                   if (!Enum.IsDefined(typeof(Shift), shift)) { throw new ArgumentOutOfRangeException(nameof(shift)); }
                                                               }
                                                           }
                                                           """, idioms: ["`!Enum.IsDefined(typeof(E), p)`, `!Enum.IsDefined(p)`"]),

        new GuardedShape("ardalis-null", "Envelope", """
                                                     public sealed class Envelope {

                                                         public string Body { get; }

                                                         public Envelope(string body) {
                                                             Body = Guard.Against.Null(body);
                                                         }

                                                     }
                                                     """, usings: "using Ardalis.GuardClauses;", idioms: ["`Guard.Against.Null(p)` / `Guard.IsNotNull(p, …)`"]),

        new GuardedShape("ardalis-zero", "Divisor", """
                                                    public sealed class Divisor {

                                                        public int Value { get; }

                                                        public Divisor(int value) {
                                                            Value = Guard.Against.Zero(value);
                                                        }

                                                    }
                                                    """, usings: "using Ardalis.GuardClauses;", idioms: ["`Guard.Against.Zero(p)`"]),

        new GuardedShape("ardalis-enum-out-of-range", "Booking", """
                                                                 public enum Cabin { Economy, Business }

                                                                 public sealed class Booking {

                                                                     public Cabin Cabin { get; }

                                                                     public Booking(Cabin cabin) {
                                                                         Cabin = Guard.Against.EnumOutOfRange(cabin);
                                                                     }

                                                                 }
                                                                 """, usings: "using Ardalis.GuardClauses;", idioms: ["`Guard.Against.EnumOutOfRange(p)`, **where `p` is of the enum's own type**"]),

        new GuardedShape("ardalis-default", "Shipment", """
                                                       public sealed class Shipment {

                                                           public Guid Id { get; }

                                                           public int Weight { get; }

                                                           public Shipment(Guid id, int weight) {
                                                               Id     = Guard.Against.Default(id);
                                                               Weight = Guard.Against.Default(weight);
                                                           }

                                                       }
                                                       """, usings: "using Ardalis.GuardClauses;", idioms: ["`Guard.Against.Default(p)` on a `Guid`; on a number"]),

        new GuardedShape("toolkit-strict-bounds", "Reading2", """
                                                              public sealed class Reading2 {

                                                                  private readonly int celsius;

                                                                  public Reading2(int celsius) {
                                                                      Guard.IsGreaterThan(celsius, 0);
                                                                      Guard.IsLessThan(celsius, 100);
                                                                      this.celsius = celsius;
                                                                  }

                                                              }
                                                              """, usings: "using CommunityToolkit.Diagnostics;", idioms: ["`Guard.IsGreaterThan(p, min)` / `Guard.IsLessThan(p, max)` — strict, measured"]),

        new GuardedShape("toolkit-inclusive-bounds", "Level", """
                                                             public sealed class Level {

                                                                 private readonly int value;

                                                                 public Level(int value) {
                                                                     Guard.IsGreaterThanOrEqualTo(value, 1);
                                                                     Guard.IsLessThanOrEqualTo(value, 9);
                                                                     this.value = value;
                                                                 }

                                                             }
                                                             """, usings: "using CommunityToolkit.Diagnostics;", idioms: ["`Guard.IsGreaterThanOrEqualTo(p, min)` / `Guard.IsLessThanOrEqualTo(p, max)`"]),

        // ---- The exclusive edge of a library bound is observable in exactly ONE place: where a floor and
        // ---- a ceiling meet at the SAME number, which is the only case `Admits` reads `Exclusive` in.
        // ---- Anywhere else `> 5` and `>= 5` leave a generator the same values to draw, so a row that quietly
        // ---- lost its strictness would read identically and every shape above would stay green. One shape
        // ---- per row that carries the flag, each admitting nothing — and each admitting exactly one value if
        // ---- the flag goes.

        new GuardedShape("toolkit-strict-floor-meets-ceiling", "Aperture", """
                                                                           public sealed class Aperture {

                                                                               private readonly int stop;

                                                                               public Aperture(int stop) {
                                                                                   Guard.IsGreaterThan(stop, 5);
                                                                                   Guard.IsLessThanOrEqualTo(stop, 5);
                                                                                   this.stop = stop;
                                                                               }

                                                                           }
                                                                           """, usings: "using CommunityToolkit.Diagnostics;", beyondTheEngine: true),

        new GuardedShape("toolkit-inclusive-floor-meets-strict-ceiling", "Notch", """
                                                                                  public sealed class Notch {

                                                                                      private readonly int depth;

                                                                                      public Notch(int depth) {
                                                                                          Guard.IsGreaterThanOrEqualTo(depth, 7);
                                                                                          Guard.IsLessThan(depth, 7);
                                                                                          this.depth = depth;
                                                                                      }

                                                                                  }
                                                                                  """, usings: "using CommunityToolkit.Diagnostics;", beyondTheEngine: true),

        new GuardedShape("toolkit-range-whose-ends-meet", "Cursor", """
                                                                    public sealed class Cursor {

                                                                        private readonly int position;

                                                                        public Cursor(int position) {
                                                                            Guard.IsInRange(position, 5, 5, nameof(position));
                                                                            this.position = position;
                                                                        }

                                                                    }
                                                                    """, usings: "using CommunityToolkit.Diagnostics;", beyondTheEngine: true),

        // ---- A named argument, written OUT of its positional order — which is what makes the name do the
        // ---- work. Written in order the name and the position agree, and slotting cannot be caught getting
        // ---- it wrong; here they disagree, so ignoring the name lands the bounds crossed and the domain
        // ---- admits nothing. `TryBound`'s own remark claims a named argument reads the same as a positional
        // ---- one, and until this shape no row passed one at all.

        new GuardedShape("toolkit-named-bounds-out-of-order", "Window", """
                                                                        public sealed class Window {

                                                                            private readonly int width;

                                                                            public Window(int width) {
                                                                                Guard.IsInRange(width, maximum: 20, minimum: 10);
                                                                                this.width = width;
                                                                            }

                                                                        }
                                                                        """, usings: "using CommunityToolkit.Diagnostics;"),

        // ---- The two bounds §5.3 cannot carry, both documented on `TryRead` and neither exercised: a
        // ---- constant that is not a number, and an expression that is not a constant. Each must leave the
        // ---- guard UNREAD rather than read as something else, so each blocks compilation (§5.6). The first
        // ---- bound is deliberately one no value satisfies, which makes the pin these rows carry — that the
        // ---- base kept underneath is rejected by the real constructor — a fact here rather than a
        // ---- probability.

        new GuardedShape("toolkit-bound-that-is-not-a-number", "Sku", """
                                                                      public sealed class Sku {

                                                                          private readonly string code;

                                                                          public Sku(string code) {
                                                                              Guard.IsLessThan(code, "");
                                                                              this.code = code;
                                                                          }

                                                                      }
                                                                      """, usings: "using CommunityToolkit.Diagnostics;", requiresVerification: true),

        new GuardedShape("toolkit-bound-that-is-not-a-constant", "Quota", """
                                                                          public sealed class Quota {

                                                                              private static readonly int Floor = 10;

                                                                              private readonly int amount;

                                                                              public Quota(int amount) {
                                                                                  Guard.IsGreaterThan(amount, Floor);
                                                                                  this.amount = amount;
                                                                              }

                                                                          }
                                                                          """, usings: "using CommunityToolkit.Diagnostics;", requiresVerification: true),

        // ---- Two idioms whose only shapes were shapes that deliberately do not read them: one beyond the
        // ---- engine, one at a position the engine cannot vouch for. Each gets a shape that does read it,
        // ---- so the claim rests on a scaffold the engine actually constrains.

        new GuardedShape("bcl-exact-length", "Code", """
                                                     public sealed class Code {
                                                         public Code(string value) {
                                                             if (value.Length != 6) { throw new ArgumentException(nameof(value)); }
                                                         }
                                                     }
                                                     """, idioms: ["`p.Length != N`"]),

        new GuardedShape("ardalis-negative-or-zero", "Allowance", """
                                                                  public sealed class Allowance {

                                                                      public decimal Amount { get; }

                                                                      public Allowance(decimal amount) {
                                                                          Amount = Guard.Against.NegativeOrZero(amount);
                                                                      }

                                                                  }
                                                                  """, usings: "using Ardalis.GuardClauses;", idioms: ["`Guard.Against.NegativeOrZero(p)`"]),


        // ---- The exclusive edge again, on the two paths `LibraryGuards` does not cover. `Admits` reads
        // ---- `Exclusive` in exactly one place — where a floor and a ceiling meet at the SAME number — so a
        // ---- row that quietly lost its strictness reads identically everywhere else. Four rows carry the
        // ---- flag here: the two BCL throw helpers whose names say "or equal", and the two sign rows. Each
        // ---- shape below admits nothing, and admits exactly one value if the flag goes.

        new GuardedShape("bcl-strict-floor-meets-ceiling", "Cadence", """
                                                                      public sealed class Cadence {

                                                                          private readonly int beats;

                                                                          public Cadence(int beats) {
                                                                              ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(beats, 5);
                                                                              if (beats > 5) { throw new ArgumentOutOfRangeException(nameof(beats)); }
                                                                              this.beats = beats;
                                                                          }

                                                                      }
                                                                      """, beyondTheEngine: true),

        new GuardedShape("bcl-strict-ceiling-meets-floor", "Grade", """
                                                                    public sealed class Grade {

                                                                        private readonly int score;

                                                                        public Grade(int score) {
                                                                            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(score, 7);
                                                                            if (score < 7) { throw new ArgumentOutOfRangeException(nameof(score)); }
                                                                            this.score = score;
                                                                        }

                                                                    }
                                                                    """, beyondTheEngine: true),

        new GuardedShape("positive-meets-a-ceiling-at-zero", "Weight", """
                                                                       public sealed class Weight {

                                                                           private readonly int grams;

                                                                           public Weight(int grams) {
                                                                               if (grams <= 0) { throw new ArgumentOutOfRangeException(nameof(grams)); }
                                                                               if (grams > 0) { throw new ArgumentOutOfRangeException(nameof(grams)); }
                                                                               this.grams = grams;
                                                                           }

                                                                       }
                                                                       """, beyondTheEngine: true),

        new GuardedShape("negative-meets-a-floor-at-zero", "Delta", """
                                                                    public sealed class Delta {

                                                                        private readonly int change;

                                                                        public Delta(int change) {
                                                                            if (change >= 0) { throw new ArgumentOutOfRangeException(nameof(change)); }
                                                                            if (change < 0) { throw new ArgumentOutOfRangeException(nameof(change)); }
                                                                            this.change = change;
                                                                        }

                                                                    }
                                                                    """, beyondTheEngine: true),

        // ---- The comparison written the other way round — the parameter on the RIGHT. The reader has a whole
        // ---- path for it (it records that the sides are flipped, and flips the operator back), and until this
        // ---- shape nothing in the corpus took that path. A reader that forgot to flip would read a ceiling as
        // ---- a floor, which is not a lost constraint but an inverted one.

        new GuardedShape("flipped-comparison", "Tempo", """
                                                        public sealed class Tempo {

                                                            private readonly int beats;

                                                            public Tempo(int beats) {
                                                                if (200 < beats) { throw new ArgumentOutOfRangeException(nameof(beats)); }
                                                                this.beats = beats;
                                                            }

                                                        }
                                                        """),

        // ---- An unsigned parameter carrying the sign guard that cannot be honoured on it. `p >= 0` says
        // ---- "must be negative", and no unsigned value is: the row has no member to write and no draw would
        // ---- satisfy it, so the engine must refuse rather than emit a Negative the generator would drop
        // ---- (ADR-0046). No corpus shape had an unsigned parameter at all.

        new GuardedShape("unsigned-cannot-be-negative", "Counter", """
                                                                   public sealed class Counter {

                                                                       private readonly uint ticks;

                                                                       public Counter(uint ticks) {
                                                                           if (ticks >= 0) { throw new ArgumentOutOfRangeException(nameof(ticks)); }
                                                                           this.ticks = ticks;
                                                                       }

                                                                   }
                                                                   """, requiresVerification: true),

        // ---- A bound that is a floating-point constant with no place on the number line. `TryDecimal` refuses
        // ---- NaN and the infinities in its first two lines, before any conversion, and nothing had ever handed
        // ---- it one: the guard must read as unvouched-for rather than as a bound at zero. Two parameters,
        // ---- because those are two separate lines — one asks it of a `double` constant, one of a `float`.
        // ---- Compared as `less than` on purpose. The first draft compared the other way, and nothing is above
        // ---- an infinity, so both guards were vacuous: the kept base drew two hundred values its own
        // ---- constructor accepted, and this row's own pin caught it. These reject every finite value.

        new GuardedShape("bound-that-is-not-on-the-number-line", "Ratio", """
                                                                          public sealed class Ratio {

                                                                              private readonly double factor;

                                                                              private readonly float scale;

                                                                              public Ratio(double factor, float scale) {
                                                                                  if (factor < double.PositiveInfinity) { throw new ArgumentOutOfRangeException(nameof(factor)); }
                                                                                  if (scale < float.PositiveInfinity) { throw new ArgumentOutOfRangeException(nameof(scale)); }
                                                                                  this.factor = factor;
                                                                                  this.scale  = scale;
                                                                              }

                                                                          }
                                                                          """, requiresVerification: true),
    ];

    /// <summary>The shape names, as the theory rows carry them.</summary>

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
                              string? usings = null,
                              IReadOnlyList<string>? idioms = null) {
            Name                 = name;
            Target               = target;
            Domain               = Preambled(usings) + declarations;
            Defect               = defect;
            BeyondTheEngine      = beyondTheEngine;
            RequiresVerification = requiresVerification;
            Idioms               = idioms ?? [];
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
        ///     The rows of the specification's closed idiom tables (§5.3) this shape exercises, each named by
        ///     the verbatim text of the row's first column.
        /// </summary>
        /// <remarks>
        ///     A claim, read by <c>RecognisedIdiomCoverageTests</c>: a table row nothing claims is a cell of
        ///     the closed surface no one has been to, which is where every defect of the guard-reading
        ///     campaign was found. Keying on the row's own text rather than an invented identifier is
        ///     deliberate — reword the row and the claim breaks, which is the moment to re-read it.
        /// </remarks>
        internal IReadOnlyList<string> Idioms { get; }

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
