using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using JustDummies.GenDummy.UnitTests.Sweep;

using Collection = JustDummies.GenDummy.UnitTests.Sweep.SweepAxes.Collection;
using Element = JustDummies.GenDummy.UnitTests.Sweep.SweepAxes.Element;
using SizeGuard = JustDummies.GenDummy.UnitTests.Sweep.SweepAxes.SizeGuard;

namespace JustDummies.GenDummy.UnitTests.Sweep;

/// <summary>
///     The product of <see cref="SweepAxes" />, family by family, as domains a developer could have written.
/// </summary>
/// <remarks>
///     Every shape here is C# that compiles on its own, and rule 0 of <see cref="SweepOracle" /> proves it
///     before anything else runs. That order is the whole lesson of the August survey: 208 of its 4394 rows
///     were its own invalid domains — an array asked for <c>.Count</c> — reported as engine defects. A bench
///     that does not validate its inputs first cannot tell its own bugs from findings, and will publish the
///     former as the latter.
/// </remarks>
internal static class SweepShapes {

    private const int Unknown = SweepAxes.Unknown;

    // Built on first use rather than in a field initializer: the product reads the axis tables declared
    // further down this file, and a static field initializer would run before they exist.
    private static readonly Lazy<IReadOnlyList<SweepShape>> Product = new(() => [.. Produce()]);

    private static readonly Lazy<IReadOnlyList<SweepShape>> Slice = new(() => [.. Cover()]);

    /// <summary>Every shape, in a fixed order, so two runs name the same shape by the same name.</summary>
    internal static IReadOnlyList<SweepShape> All => Product.Value;

    /// <summary>
    ///     The smallest prefix-greedy subset that still touches every axis value the product uses.
    /// </summary>
    /// <remarks>
    ///     This one runs on every build, and that is the point of it. The full product is a weekly job, and an
    ///     instrument exercised once a week breaks in silence for six days — which is the failure this whole
    ///     campaign keeps repairing on other instruments. The slice cannot find what the product finds; it
    ///     proves that the product would still run.
    /// </remarks>
    internal static IReadOnlyList<SweepShape> CoveringSlice => Slice.Value;

    /// <summary>The shape a test row names.</summary>
    internal static SweepShape Named(string name) {
        return All.Single(shape => shape.Name == name);
    }

    private static IEnumerable<SweepShape> Cover() {
        HashSet<string> seen = [];

        foreach (SweepShape shape in All) {
            string[] tokens = [shape.Family, .. shape.Name.Split('-')];

            if (tokens.Any(token => seen.Add(token))) { yield return shape; }
        }
    }

    private static IEnumerable<SweepShape> Produce() {
        foreach (SweepShape shape in Bare()) { yield return shape; }
        foreach (SweepShape shape in Baseline()) { yield return shape; }
        foreach (SweepShape shape in Counted()) { yield return shape; }
        foreach (SweepShape shape in ElementGuards()) { yield return shape; }
        foreach (SweepShape shape in Composition()) { yield return shape; }
        foreach (SweepShape shape in Delegation()) { yield return shape; }
        foreach (SweepShape shape in DelegatedCount()) { yield return shape; }
        foreach (SweepShape shape in Nested()) { yield return shape; }
        foreach (SweepShape shape in Strings()) { yield return shape; }
    }

    // ---- bare: one scalar parameter, no collection and no guard. --------------------------------------
    //
    // The floor of the whole sweep, and the only place the cardinality rule bites on a scalar: an enum with
    // no members admits no value at all, so the engine must refuse rather than construct.

    private static IEnumerable<SweepShape> Bare() {
        foreach (Element element in SweepAxes.Elements) {
            string name   = $"bare-{element.Name}";
            string target = Identifier(name);

            yield return Shape(name, "bare", target, $$"""
                                                       public sealed class {{target}} {

                                                           private readonly {{element.Type}} value;

                                                           public {{target}}({{element.Type}} value) {
                                                               this.value = value;
                                                           }

                                                       }
                                                       """,
                               distinctDemand: 1,
                               distinctCapacity: element.Cardinality,
                               element: element);
        }
    }

    // ---- baseline: the collection, unguarded. -------------------------------------------------------
    //
    // What size the library picks for an unguarded collection is its own business, so nothing here is judged
    // on distinctness — only on the five rules that hold whatever it picks.

    private static IEnumerable<SweepShape> Baseline() {
        foreach (Collection collection in SweepAxes.Collections) {
            foreach (Element element in SweepAxes.Elements) {
                string name   = $"base-{collection.Name}-{element.Name}";
                string target = Identifier(name);

                yield return Shape(name, "baseline", target, $$"""
                                                              public sealed class {{target}} {

                                                                  private readonly {{collection.TypeOf(element)}} items;

                                                                  public {{target}}({{collection.TypeOf(element)}} items) {
                                                                      this.items = items;
                                                                  }

                                                              }
                                                              """,
                                                   element: element);
            }
        }
    }

    // ---- count / count-distinct: a size guard over the collection. ----------------------------------
    //
    // Two families out of one product, and the collection decides which: a set or a dictionary's keys hold
    // each element at most once, so a floor of N over them demands N DISTINCT values and the element's
    // cardinality settles whether that is possible. A list demands nothing of the kind, which is why the
    // element axis is spent narrow there and wide here.
    //
    // This subsumes the August `composed-distinct` family entire: `composed-iset-slot-floor4` and
    // `count-iset-slot-floor4` are the same domain, and one name for it is enough.

    private static IEnumerable<SweepShape> Counted() {
        foreach (Collection collection in SweepAxes.Collections) {
            IReadOnlyList<Element> elements = collection.Distinct ? SweepAxes.Elements : SweepAxes.CoreElements;

            foreach (Element element in elements) {
                foreach (SizeGuard guard in SweepAxes.SizeGuards) { yield return Counted(collection, element, guard); }
            }
        }
    }

    private static SweepShape Counted(Collection collection, Element element, SizeGuard guard) {
        string name   = $"count-{collection.Name}-{element.Name}-{guard.Name}";
        string target = Identifier(name);
        string type   = collection.TypeOf(element);

        return Shape(name, collection.Distinct ? "count-distinct" : "count", target, $$"""
                                                                                      public sealed class {{target}} {

                                                                                          private readonly {{type}} items;

                                                                                          public {{target}}({{type}} items) {
                                                                                      {{Guarded(guard, "items", collection.CountMember)}}
                                                                                              this.items = items;
                                                                                          }

                                                                                      }
                                                                                      """,
                     distinctDemand: collection.Distinct ? guard.Demand : 0,
                     distinctCapacity: element.Cardinality,
                     element: element);
    }

    // ---- element: a guard on what the collection HOLDS rather than on how many. ----------------------

    private static IEnumerable<SweepShape> ElementGuards() {
        foreach (Collection collection in SweepAxes.Collections) {
            if (collection.Name is "idict" or "rodict" or "dict" or "valdict") { continue; }

            foreach (Element element in SweepAxes.CoreElements) {
                string type = collection.TypeOf(element);

                string distinctName   = $"element-{collection.Name}-{element.Name}-distinct";
                string distinctTarget = Identifier(distinctName);

                yield return Shape(distinctName, "element", distinctTarget, $$"""
                                                                             public sealed class {{distinctTarget}} {

                                                                                 public {{distinctTarget}}({{type}} items) {
                                                                                     if (items.Distinct().Count() != items.{{collection.CountMember}}) { throw new ArgumentException(nameof(items)); }
                                                                                 }

                                                                             }
                                                                             """,
                                   element: element);

                // `item == null` does not compile against a non-nullable value type, and emitting it anyway
                // is how the August survey manufactured its own CS0019 rows.
                if (!element.Nullable) { continue; }

                string nullName   = $"element-{collection.Name}-{element.Name}-anynull";
                string nullTarget = Identifier(nullName);

                yield return Shape(nullName, "element", nullTarget, $$"""
                                                                     public sealed class {{nullTarget}} {

                                                                         public {{nullTarget}}({{type}} items) {
                                                                             foreach ({{element.Type}} item in items) {
                                                                                 if (item == null) { throw new ArgumentException(nameof(items)); }
                                                                             }
                                                                         }

                                                                     }
                                                                     """,
                                   element: element);
            }
        }
    }

    // ---- composition: the guarded collection sits N value objects deep. -----------------------------

    private static readonly IReadOnlyList<string> CompositionVariants = ["clean", "floor3", "unreadable"];

    private static IEnumerable<SweepShape> Composition() {
        foreach (int depth in new[] { 1, 2, 3 }) {
            foreach (Collection collection in Named(SweepAxes.Collections, "rolist", "iset", "array")) {
                foreach (Element element in Named(SweepAxes.Elements, "slot", "code", "int")) {
                    foreach (string variant in CompositionVariants) { yield return Composed(depth, collection, element, variant); }
                }
            }
        }
    }

    private static SweepShape Composed(int depth, Collection collection, Element element, string variant) {
        string name   = $"depth{depth}-{collection.Name}-{element.Name}-{variant}";
        string target = Identifier(name);
        string type   = collection.TypeOf(element);

        StringBuilder declarations = new();

        declarations.Append(CultureInfo.InvariantCulture, $$"""
                                                          public sealed class {{target}} {

                                                              public {{target}}({{target}}Level1 level) { }

                                                          }
                                                          """);

        for (int level = 1; level < depth; level++) {
            declarations.Append("\n\n").Append(CultureInfo.InvariantCulture, $$"""
                                                                              public sealed class {{target}}Level{{level}} {

                                                                                  public {{target}}Level{{level}}({{target}}Level{{level + 1}} level) { }

                                                                              }
                                                                              """);
        }

        declarations.Append("\n\n").Append(CultureInfo.InvariantCulture, $$"""
                                                                          public sealed class {{target}}Level{{depth}} {

                                                                              public {{target}}Level{{depth}}({{type}} items) {
                                                                          {{LeafGuard(variant, collection)}}
                                                                              }

                                                                          }
                                                                          """);

        return Shape(name, "composition", target, declarations.ToString(),
                     distinctDemand: collection.Distinct && variant == "floor3" ? 3 : 0,
                     distinctCapacity: element.Cardinality,
                     element: element,
                     alsoScaffolded: [.. Enumerable.Range(1, depth).Select(level => $"{target}Level{level}")]);
    }

    private static string LeafGuard(string variant, Collection collection) {
        return variant switch {
            "floor3" => $"        if (items.{collection.CountMember} < 3) {{ throw new ArgumentException(nameof(items)); }}",
            // No row of the closed table says this, and the engine must say so rather than guess (§5.6).
            "unreadable" => $"        if (items.{collection.CountMember} % 3 == 1) {{ throw new ArgumentException(nameof(items)); }}",
            _ => "        // no guard."
        };
    }

    // ---- delegation: the guard is reached through a hop the engine has to follow. --------------------

    private static readonly IReadOnlyList<string> Delegations = ["this", "factory", "computed"];

    /// <summary>
    ///     Why the `computed` hop is expected to draw a value its own domain rejects.
    /// </summary>
    /// <remarks>
    ///     The condition tests a local, so the only place the statement names the parameter is inside the
    ///     `nameof` of its own message — which §5.3 excludes by name, since it labels the rejected parameter
    ///     for a reader rather than testing anything. And the statement that produced the local uses its
    ///     result, so it is production rather than a guard by the same section's one structural test. The
    ///     engine therefore marks nothing, blocks nothing, and draws freely: the residue §9 calls the doubt
    ///     the tool never sees. Sixteen shapes sit here on purpose, so the size of that residue is a number
    ///     rather than an impression.
    /// </remarks>
    private const string ComputedResidue = "the guard tests a local holding items.Count, so the condition names "
                                         + "no parameter and the statement that made it uses its result: §9's "
                                         + "declared residue, not a defect.";

    private static IEnumerable<SweepShape> Delegation() {
        foreach (string hop in Delegations) {
            foreach (Collection collection in Named(SweepAxes.Collections, "rolist", "array", "iset", "idict")) {
                foreach (Element element in Named(SweepAxes.Elements, "slot", "int", "code", "string")) {
                    yield return Delegated(hop, collection, element);
                }
            }
        }
    }

    private static SweepShape Delegated(string hop, Collection collection, Element element) {
        string name   = $"delegate-{hop}-{collection.Name}-{element.Name}";
        string target = Identifier(name);
        string type   = collection.TypeOf(element);
        string guard  = $"        if (items.{collection.CountMember} < 2) {{ throw new ArgumentException(nameof(items)); }}";

        string body = hop switch {
            "this" => $$"""
                        public sealed class {{target}} {

                            public {{target}}({{type}} items) : this(items, 0) { }

                            private {{target}}({{type}} items, int reserved) {
                        {{guard}}
                            }

                        }
                        """,
            "factory" => $$"""
                           public sealed class {{target}} {

                               private {{target}}({{type}} items) {
                           {{guard}}
                               }

                               public static {{target}} Create({{type}} items) { return new {{target}}(items); }

                           }
                           """,
            _ => $$"""
                   public sealed class {{target}} {

                       public {{target}}({{type}} items) {
                           int size = items.{{collection.CountMember}};

                           if (size < 2) { throw new ArgumentException(nameof(items)); }
                       }

                   }
                   """
        };

        return Shape(name, "delegation", target, body,
                     distinctDemand: collection.Distinct ? 2 : 0,
                     distinctCapacity: element.Cardinality,
                     element: element,
                     residue: hop == "computed" ? ComputedResidue : null);
    }

    // ---- delegated-count: the same hop, crossed with the size guards instead of with the collections. -
    //
    // One of the three families still standing among Guards.cs's surviving mutants: a size bound handed to a
    // delegated constructor is a bound nothing in this repository had ever asked the engine to follow.

    private static IEnumerable<SweepShape> DelegatedCount() {
        foreach (string hop in new[] { "factory", "hop" }) {
            foreach (Element element in Named(SweepAxes.Elements, "bool", "slot", "suit", "wide", "int", "code")) {
                foreach (SizeGuard guard in Named(SweepAxes.SizeGuards, "floor1", "floor3", "floor5", "exact2", "ceil3", "range2to5")) {
                    string name   = $"delegated-{hop}-{element.Name}-{guard.Name}";
                    string target = Identifier(name);
                    string type   = $"ISet<{element.Type}>";
                    string body   = Guarded(guard, "items", "Count");

                    string declarations = hop == "factory"
                                              ? $$"""
                                                  public sealed class {{target}} {

                                                      private {{target}}({{type}} items) {
                                                  {{body}}
                                                      }

                                                      public static {{target}} Create({{type}} items) { return new {{target}}(items); }

                                                  }
                                                  """
                                              : $$"""
                                                  public sealed class {{target}} {

                                                      public {{target}}({{type}} items) : this(items, 0) { }

                                                      private {{target}}({{type}} items, int reserved) {
                                                  {{body}}
                                                      }

                                                  }
                                                  """;

                    yield return Shape(name, "delegated-count", target, declarations,
                                       distinctDemand: guard.Demand,
                                       distinctCapacity: element.Cardinality,
                                       element: element);
                }
            }
        }
    }

    // ---- nested: a collection of collections, guarded on the outer size. ----------------------------

    private static IEnumerable<SweepShape> Nested() {
        foreach (Collection outer in Named(SweepAxes.Collections, "rolist", "array", "list", "icoll")) {
            foreach (Collection inner in Named(SweepAxes.Collections, "iset", "rolist", "array")) {
                foreach (Element element in Named(SweepAxes.Elements, "slot", "int", "code")) {
                    foreach (SizeGuard guard in Named(SweepAxes.SizeGuards, "floor1", "floor3", "exact2", "ceil3")) {
                        string name   = $"nested-{outer.Name}-{inner.Name}of{element.Name}-{guard.Name}";
                        string target = Identifier(name);
                        string type   = string.Format(CultureInfo.InvariantCulture, outer.Form, inner.TypeOf(element));

                        yield return Shape(name, "nested", target, $$"""
                                                                    public sealed class {{target}} {

                                                                        public {{target}}({{type}} items) {
                                                                    {{Guarded(guard, "items", outer.CountMember)}}
                                                                        }

                                                                    }
                                                                    """,
                                                         element: element);
                    }
                }
            }
        }
    }

    // ---- string: the idioms §5.3 closes over a string, alone and inside a set. -----------------------

    private static readonly IReadOnlyList<(string Name, string Condition)> StringGuards = [
        ("isnullorwhitespace", "string.IsNullOrWhiteSpace({0})"),
        ("isnullorempty", "string.IsNullOrEmpty({0})"),
        ("lengthfloor", "{0}.Length < 8"),
        ("lengthceil", "{0}.Length > 20"),
        ("lengthexact", "{0}.Length != 12"),
        ("trimlength", "{0}.Trim().Length == 0"),
        ("startswith", "!{0}.StartsWith(\"R-\", StringComparison.Ordinal)"),
        ("nullcheck", "{0} is null")
    ];

    private static IEnumerable<SweepShape> Strings() {
        foreach ((string guardName, string condition) in StringGuards) {
            string name   = $"string-{guardName}";
            string target = Identifier(name);
            string test   = string.Format(CultureInfo.InvariantCulture, condition, "value");

            yield return Shape(name, "string", target, $$"""
                                                        public sealed class {{target}} {

                                                            public {{target}}(string value) {
                                                                if ({{test}}) { throw new ArgumentException(nameof(value)); }
                                                            }

                                                        }
                                                        """);

            string setName   = $"string-set-{guardName}";
            string setTarget = Identifier(setName);
            string setTest   = string.Format(CultureInfo.InvariantCulture, condition, "item");

            yield return Shape(setName, "string", setTarget, $$"""
                                                              public sealed class {{setTarget}} {

                                                                  public {{setTarget}}(ISet<string> items) {
                                                                      foreach (string item in items) {
                                                                          if ({{setTest}}) { throw new ArgumentException(nameof(items)); }
                                                                      }
                                                                  }

                                                              }
                                                              """);
        }
    }

    // ---- the plumbing. -------------------------------------------------------------------------------

    private static SweepShape Shape(string name,
                                    string family,
                                    string target,
                                    string declarations,
                                    int distinctDemand = 0,
                                    int distinctCapacity = Unknown,
                                    Element? element = null,
                                    IReadOnlyList<string>? alsoScaffolded = null,
                                    string? residue = null) {
        string domain = SweepVocabulary.Preamble + SweepVocabulary.Declarations + "\n\n" + declarations + "\n";

        return new SweepShape(name, family, target, domain, distinctDemand, distinctCapacity,
                              [.. alsoScaffolded ?? [], .. Beside(element)], residue, element);
    }

    /// <summary>The generator the element needs of its own, when it needs one.</summary>
    private static IReadOnlyList<string> Beside(Element? element) {
        return element?.Composed == true ? [element.Type.TrimEnd('?')] : [];
    }

    /// <summary>The one or two conditions the guard declares, as a developer declares them.</summary>
    private static string Guarded(SizeGuard guard, string parameter, string countMember) {
        string first = Throws(guard.Condition, parameter, countMember);

        return guard.Second is null ? first : first + "\n" + Throws(guard.Second, parameter, countMember);
    }

    private static string Throws(string condition, string parameter, string countMember) {
        string test = string.Format(CultureInfo.InvariantCulture, condition, parameter, countMember);

        return $"        if ({test}) {{ throw new ArgumentException(nameof({parameter})); }}";
    }

    /// <summary>The C# identifier a shape's name spells — <c>count-iset-bool-floor1</c> is <c>CountIsetBoolFloor1</c>.</summary>
    private static string Identifier(string name) {
        StringBuilder identifier = new();

        foreach (string part in name.Split('-')) {
            if (part.Length == 0) { continue; }

            identifier.Append(char.ToUpperInvariant(part[0])).Append(part, 1, part.Length - 1);
        }

        return identifier.ToString();
    }

    private static IEnumerable<Collection> Named(IReadOnlyList<Collection> axis, params string[] names) {
        return names.Select(name => axis.Single(entry => entry.Name == name));
    }

    private static IEnumerable<Element> Named(IReadOnlyList<Element> axis, params string[] names) {
        return names.Select(name => axis.Single(entry => entry.Name == name));
    }

    private static IEnumerable<SizeGuard> Named(IReadOnlyList<SizeGuard> axis, params string[] names) {
        return names.Select(name => axis.Single(entry => entry.Name == name));
    }

}
