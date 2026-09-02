using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace JustDummies.GenAny.UnitTests.Sweep;

/// <summary>
///     The axes the sweep takes the product of, declared rather than discovered.
/// </summary>
/// <remarks>
///     The sweep is an enumeration, not a fuzzer: the same axes give the same shapes in the same order on
///     every machine, so a finding names a shape a reader can reproduce by typing its name. What varies
///     between two runs is the engine, which is the point.
/// </remarks>
internal static class SweepAxes {

    /// <summary>
    ///     How many distinct values the library can draw for an element, when the generated source itself
    ///     fixes that number.
    /// </summary>
    /// <remarks>
    ///     Only an enum's own member list settles this, and it settles it exactly: the count is a fact of the
    ///     domain the sweep wrote, not an assumption about how <c>Any</c> draws. Everything else —
    ///     <c>byte</c>, <c>char</c>, a composed type — is <see cref="Unknown" />, and the distinctness rule
    ///     declines to judge it rather than guessing at an alphabet. A bench that guessed here would report
    ///     its own ignorance as a defect, which is the exact failure this sweep replaces.
    /// </remarks>
    internal const int Unknown = -1;

    /// <summary>The element types, and what the source says about how many distinct values each holds.</summary>
    /// <remarks>
    ///     A nullable element carries its underlying type's cardinality, not one more: the engine never draws
    ///     null for a nullable parameter (ADR-0064), so the null is not a value the collection can hold.
    /// </remarks>
    internal static IReadOnlyList<Element> Elements { get; } = [
        new Element("bool", "bool", 2),
        new Element("nbool", "bool?", 2),
        new Element("slot", "Slot", 3),
        new Element("nslot", "Slot?", 3),
        new Element("suit", "Suit", 4),
        new Element("nsuit", "Suit?", 4),
        // Five names over three values: a generator that counted names would claim a capacity it lacks.
        new Element("grade", "Grade", 3),
        new Element("ngrade", "Grade?", 3),
        new Element("access", "Permission", 3),
        new Element("naccess", "Permission?", 3),
        new Element("wide", "Wide", 32),
        new Element("nwide", "Wide?", 32),
        new Element("one", "Lone", 1),
        new Element("none", "Nothing", 0),
        new Element("nnone", "Nothing?", 0),
        new Element("byte", "byte", Unknown),
        new Element("nbyte", "byte?", Unknown),
        new Element("sbyte", "sbyte", Unknown),
        new Element("char", "char", Unknown),
        new Element("nchar", "char?", Unknown),
        new Element("int", "int", Unknown),
        new Element("string", "string", Unknown),
        new Element("code", "Code", Unknown),
        new Element("tag", "Tag", Unknown),
        new Element("delta", "Delta", Unknown),
        new Element("badge", "Badge", Unknown),
        new Element("stamp", "Stamp", Unknown),
        new Element("doubtful", "Doubtful", Unknown)
    ];

    /// <summary>
    ///     The elements every collection is crossed with, where the element type is not what the row is about.
    /// </summary>
    /// <remarks>
    ///     A count guard on a list interacts with the element only through distinctness, and a list demands
    ///     none — so the full element axis there would buy breadth the run pays for and nobody reads. It is
    ///     spent on the distinct collections instead, where cardinality decides the answer.
    /// </remarks>
    internal static IReadOnlyList<Element> CoreElements { get; } =
        [.. Named("bool", "int", "string", "suit", "code", "delta", "badge", "doubtful")];

    /// <summary>The collection types, and how a developer asks each one for its size.</summary>
    /// <remarks>
    ///     <see cref="Collection.CountMember" /> is not decoration. An array answers <c>Length</c> and not
    ///     <c>Count</c>, and a domain that asks an array for <c>.Count</c> does not compile — which is how the
    ///     August fuzzer produced 208 rows of its own invalid C# and read them as engine defects. Carrying the
    ///     member on the axis is what makes rule 0 able to hold.
    ///     <para>
    ///         A dictionary appears twice on purpose: keyed BY the element, where the keys are distinct and
    ///         cardinality decides, and keyed by <c>int</c> with the element as the VALUE, where it does not.
    ///     </para>
    /// </remarks>
    internal static IReadOnlyList<Collection> Collections { get; } = [
        new Collection("rolist", "IReadOnlyList<{0}>", "Count", distinct: false),
        new Collection("array", "{0}[]", "Length", distinct: false),
        new Collection("list", "List<{0}>", "Count", distinct: false),
        new Collection("icoll", "ICollection<{0}>", "Count", distinct: false),
        new Collection("valdict", "IDictionary<int, {0}>", "Count", distinct: false),
        new Collection("iset", "ISet<{0}>", "Count", distinct: true),
        new Collection("hashset", "HashSet<{0}>", "Count", distinct: true),
        new Collection("idict", "IDictionary<{0}, int>", "Count", distinct: true),
        new Collection("rodict", "IReadOnlyDictionary<{0}, int>", "Count", distinct: true),
        new Collection("dict", "Dictionary<{0}, int>", "Count", distinct: true)
    ];

    /// <summary>The size guards, and how many distinct elements each one demands at least.</summary>
    internal static IReadOnlyList<SizeGuard> SizeGuards { get; } = [
        new SizeGuard("nonempty", "{0}.{1} == 0", demand: 1),
        new SizeGuard("floor1", "{0}.{1} < 1", demand: 1),
        new SizeGuard("floor2", "{0}.{1} < 2", demand: 2),
        new SizeGuard("floor3", "{0}.{1} < 3", demand: 3),
        new SizeGuard("floor4", "{0}.{1} < 4", demand: 4),
        new SizeGuard("floor5", "{0}.{1} < 5", demand: 5),
        new SizeGuard("exact1", "{0}.{1} != 1", demand: 1),
        new SizeGuard("exact2", "{0}.{1} != 2", demand: 2),
        new SizeGuard("exact3", "{0}.{1} != 3", demand: 3),
        new SizeGuard("exact4", "{0}.{1} != 4", demand: 4),
        new SizeGuard("ceil1", "{0}.{1} > 1", demand: 0),
        new SizeGuard("ceil3", "{0}.{1} > 3", demand: 0),
        new SizeGuard("ceil8", "{0}.{1} > 8", demand: 0),
        new SizeGuard("ceil129", "{0}.{1} > 129", demand: 0),
        new SizeGuard("range1to3", "{0}.{1} < 1", "{0}.{1} > 3", demand: 1),
        new SizeGuard("range2to5", "{0}.{1} < 2", "{0}.{1} > 5", demand: 2)
    ];

    private static IEnumerable<Element> Named(params string[] names) {
        foreach (string name in names) {
            foreach (Element element in Elements) {
                if (element.Name == name) { yield return element; }
            }
        }
    }

    /// <summary>One element type, by the name a shape carries and the C# a domain declares.</summary>
    internal sealed class Element(string name, string type, int cardinality) {

        /// <summary>The element spellings a null test does not compile against.</summary>
        private static readonly ImmutableHashSet<string> NotNullable =
            ImmutableHashSet.Create("bool", "byte", "sbyte", "char", "int",
                                    "Slot", "Suit", "Grade", "Permission", "Wide", "Lone", "Nothing");

        internal string Name { get; } = name;

        internal string Type { get; } = type;

        /// <summary>
        ///     Whether <c>item == null</c> is legal C# against this element.
        /// </summary>
        /// <remarks>
        ///     Carried rather than assumed: a null test against a non-nullable value type does not compile,
        ///     and emitting one anyway is half of how the August survey manufactured its own error rows.
        /// </remarks>
        internal bool Nullable { get; } = type.EndsWith("?", StringComparison.Ordinal) || !NotNullable.Contains(type);

        /// <summary>
        ///     Whether a parameter of this type draws through a generator of its own (§5.4, ADR-0089).
        /// </summary>
        /// <remarks>
        ///     True of the domain's classes and of nothing else: an enum is drawn by <c>Any.Enum&lt;T&gt;()</c>
        ///     and a primitive by its row of the base table, neither of which names a second generator. A
        ///     shape whose element is composed has to scaffold that generator too, or half of what lands in
        ///     the developer's project is missing.
        /// </remarks>
        internal bool Composed { get; } = !NotNullable.Contains(type.TrimEnd('?')) && type != "string";

        /// <summary>How many distinct values, or <see cref="Unknown" />.</summary>
        internal int Cardinality { get; } = cardinality;

        /// <inheritdoc />
        public override string ToString() {
            return Name;
        }

    }

    /// <summary>One collection type, and the two things a guard over it has to know.</summary>
    internal sealed class Collection(string name, string form, string countMember, bool distinct) {

        internal string Name { get; } = name;

        /// <summary>The C# type, with <c>{0}</c> where the element goes.</summary>
        internal string Form { get; } = form;

        /// <summary><c>Count</c>, or <c>Length</c> for an array.</summary>
        internal string CountMember { get; } = countMember;

        /// <summary>Whether the collection holds each element at most once — a set, or a dictionary's keys.</summary>
        internal bool Distinct { get; } = distinct;

        internal string TypeOf(Element element) {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, Form, element.Type);
        }

        /// <inheritdoc />
        public override string ToString() {
            return Name;
        }

    }

    /// <summary>One size guard, as the one or two conditions a developer writes.</summary>
    internal sealed class SizeGuard {

        internal SizeGuard(string name, string condition, int demand) : this(name, condition, second: null, demand) { }

        internal SizeGuard(string name, string condition, string? second, int demand) {
            Name      = name;
            Condition = condition;
            Second    = second;
            Demand    = demand;
        }

        internal string Name { get; }

        /// <summary>The condition, with <c>{0}</c> for the parameter and <c>{1}</c> for the count member.</summary>
        internal string Condition { get; }

        /// <summary>The second condition of a range, declared separately as a developer declares it.</summary>
        internal string? Second { get; }

        /// <summary>The fewest distinct elements a collection satisfying this guard must hold.</summary>
        internal int Demand { get; }

        /// <inheritdoc />
        public override string ToString() {
            return Name;
        }

    }

}
