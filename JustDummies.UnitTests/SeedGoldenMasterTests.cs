#region Usings declarations

using System.Globalization;
using System.Reflection;
using System.Text;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The seed golden master: what a fixed seed draws, factory by factory, pinned against a committed reference
///     file. This is the enforcement of ADR-0049 — a seed replays across patch and minor versions — and without it
///     that promise would be a sentence in a README that nothing checks, which breaks silently while consumers act
///     on it.
/// </summary>
/// <remarks>
///     <para>
///         Every case pins TWO things: the value produced, and the <b>number of draws consumed</b>. The second is
///         not redundant. Draws come from one sequential stream shared by the whole scope, so a change that leaves a
///         factory's own output identical while taking one extra draw shifts every value produced after it, in every
///         test that calls it — and a golden master watching only values stays green throughout. Pinning consumption
///         is also what keeps this suite's cases <b>independent</b>: if no factory changes either its value or its
///         draw count, then no sequence of calls can drift, whatever a caller wrote. The check stays per factory and
///         the guarantee stays global, which is what makes a combinatorial golden master over call sequences
///         unnecessary.
///     </para>
///     <para>
///         Each case runs in its OWN fresh <see cref="Dummy.UseSeed(int)" /> scope. Sharing one scope would make the
///         suite report a single change in an early factory as a change in every case after it — the cascade that
///         makes a golden master unreadable and eventually ignored.
///     </para>
///     <para>
///         The AMBIENT source is what the cases draw from, not an <see cref="Dummy.WithSeed" /> context. Both reach the
///         same generators, and the ambient path is both the one a user pins (<c>Dummy.Reproducibly</c>,
///         <c>[Reproducible]</c>) and the only one the pool factories — <see cref="Dummy.OneOf{T}(T[])" />,
///         <see cref="Dummy.Enum{TEnum}" /> — can be reached through at all, since they bind to the ambient source by
///         construction.
///     </para>
///     <para>
///         Only the surface common to both target frameworks is covered. This suite also runs on the net472 floor
///         (<c>build/Net472TestFloor.props</c>), where <c>DateOnly</c>, <c>TimeOnly</c>, <c>Int128</c>,
///         <c>UInt128</c> and <c>Half</c> do not exist; covering them would need a second reference file per leg and
///         would buy nothing this one does not, since the modern generators draw from the same source. Running on
///         both legs also means this suite re-proves, from the other side, the cross-framework seed equality that
///         <c>tools/justdummies-check</c> asserts on the packaged assets.
///     </para>
///     <para>
///         <b>When a case here goes red</b>, the mapping moved. Under ADR-0049 that is a MAJOR-version change, not a
///         file to refresh: update the reference only as part of a deliberate major, never to make a build green.
///     </para>
/// </remarks>
[TestSubject(typeof(Dummy))]
public sealed class SeedGoldenMasterTests {

    #region Statics members declarations

    /// <summary>
    ///     The seed every case draws from. Fixed and arbitrary: what matters is that it never changes, not what it
    ///     is. Deliberately NOT the <c>CrossTfmSeed</c> that <c>tools/justdummies-check</c> uses — two independent
    ///     seeds catch a drift that happens to leave one seed's sequence looking unchanged.
    /// </summary>
    private const int GoldenSeed = 20260801;

    private const string ReferenceFileName = "SeedGoldenMaster.expected.txt";

    /// <summary>
    ///     One case per factory: a name, and how to draw it. The name is the reference file's key, so renaming one
    ///     rewrites a line of the file — deliberate, since a rename is how a factory's identity changes.
    /// </summary>
    private static IEnumerable<(string Name, Func<string> Draw)> Cases() {
        // Scalars, unconstrained: the plainest draw each factory makes.
        yield return ("Boolean", () => Dummy.Boolean().Generate().ToString());
        yield return ("Byte", () => Render(Dummy.Byte().Generate()));
        yield return ("SByte", () => Render(Dummy.SByte().Generate()));
        yield return ("Int16", () => Render(Dummy.Int16().Generate()));
        yield return ("UInt16", () => Render(Dummy.UInt16().Generate()));
        yield return ("Int32", () => Render(Dummy.Int32().Generate()));
        yield return ("UInt32", () => Render(Dummy.UInt32().Generate()));
        yield return ("Int64", () => Render(Dummy.Int64().Generate()));
        yield return ("UInt64", () => Dummy.UInt64().Generate().ToString(CultureInfo.InvariantCulture));
        yield return ("Single", () => Bits(Dummy.Single().Generate()));
        yield return ("Double", () => Bits(Dummy.Double().Generate()));
        yield return ("Decimal", () => Dummy.Decimal().Generate().ToString(CultureInfo.InvariantCulture));
        yield return ("Char", () => Escape(Dummy.Char().Generate().ToString()));
        yield return ("Guid", () => Dummy.Guid().Generate().ToString());
        yield return ("TimeSpan", () => Render(Dummy.TimeSpan().Generate().Ticks));
        yield return ("DateTime", () => Render(Dummy.DateTime().Generate().Ticks));
        yield return ("String", () => Escape(Dummy.String().Generate()));
        yield return ("Uri", () => Dummy.Uri().Generate().ToString());

        // Both dimensions of one draw, from a single generated value: the instant and the offset are chosen
        // separately (ADR-0030), so rendering only the instant would leave half the draw unwatched.
        yield return ("DateTimeOffset", () => {
            DateTimeOffset drawn = Dummy.DateTimeOffset().Generate();

            return Render(drawn.Ticks) + "+" + Render(drawn.Offset.Ticks);
        });

        // Constrained scalars: a constraint can take a different draw path from the unconstrained factory, so
        // pinning only the plain form would leave the constrained ones unwatched.
        yield return ("Int32.Between", () => Render(Dummy.Int32().Between(1, 1000).Generate()));
        yield return ("Int64.Between", () => Render(Dummy.Int64().Between(-500L, 500L).Generate()));
        yield return ("Double.Between", () => Bits(Dummy.Double().Between(0d, 1000d).Generate()));
        yield return ("Decimal.Between", () => Dummy.Decimal().Between(0m, 1000m).Generate().ToString(CultureInfo.InvariantCulture));
        yield return ("String.NonEmpty.WithMaxLength", () => Escape(Dummy.String().NonEmpty().WithMaxLength(50).Generate()));
        yield return ("StringMatching", () => Escape(Dummy.StringMatching("[a-z]{3}-[0-9]{4}").Generate()));

        // The nullable wrapper: a draw of its own, on top of the operand's. A null renders as a marker no drawn
        // value can produce, so "the draw was null" and "the draw was the text of the marker" stay distinguishable.
        // Under this seed both take the null branch, so the reference pins the NULL DECISION and its single draw
        // rather than an operand value — which is the part specific to OrNull. The operand's own mapping is pinned
        // by its own case above, so nothing is left unwatched by the branch these two happen to land on.
        yield return ("Int32.OrNull", () => Dummy.Int32().Between(0, 100).OrNull().Generate() is int value ? Render(value) : NullMarker);
        yield return ("String.OrNull", () => Dummy.String().NonEmpty().WithMaxLength(8).OrNull().Generate() is string text ? Escape(text) : NullMarker);

        // Pool factories. They bind to the ambient source by construction, which is why this suite pins the ambient
        // path: reached through a context they would not be seeded at all.
        yield return ("OneOf", () => Dummy.OneOf("alpha", "beta", "gamma", "delta").Generate());
        yield return ("Enum", () => Dummy.Enum<GoldenSuit>().Generate().ToString());

        // Combinators. They inherit the seeded source through their operand, which is what makes them reproducible
        // at all — so a change to how they order or count their operand draws belongs here.
        yield return ("ArrayOf", () => Join(Dummy.ArrayOf(Dummy.Int32().Between(0, 9)).WithCount(3).Generate().Select(value => Render(value))));
        yield return ("ListOf", () => Join(Dummy.ListOf(Dummy.Int32().Between(0, 9)).WithCount(3).Generate().Select(value => Render(value))));
        yield return ("SequenceOf", () => Join(Dummy.SequenceOf(Dummy.Int32().Between(0, 9)).WithCount(2).Generate().Select(value => Render(value))));
        // Sorted, unlike the ordered containers above. A HashSet's and a Dictionary's enumeration order is not
        // contractual — it can differ between .NET Framework and modern .NET, and between .NET versions — so
        // pinning it would make this suite red on the net472 floor over a detail JustDummies does not decide.
        // What the library does decide is WHICH values are drawn, and sorting pins exactly that; the draw count
        // beside it still pins the order they were drawn in.
        yield return ("SetOf", () => JoinSorted(Dummy.SetOf(Dummy.Int32().Between(0, 99)).WithCount(3).Generate().Select(value => Render(value))));
        yield return ("DictionaryOf", () => JoinSorted(Dummy.DictionaryOf(Dummy.Int32().Between(0, 99), Dummy.Char()).WithCount(2).Generate().Select(entry => Render(entry.Key) + "=" + Escape(entry.Value.ToString()))));

        yield return ("PairOf", () => {
            (int First, char Second) pair = Dummy.PairOf(Dummy.Int32().Between(1, 9), Dummy.Char()).Generate();

            return Render(pair.First) + "," + Escape(pair.Second.ToString());
        });

        yield return ("TripleOf", () => {
            (int First, char Second, bool Third) triple = Dummy.TripleOf(Dummy.Int32().Between(1, 9), Dummy.Char(), Dummy.Boolean()).Generate();

            return Render(triple.First) + "," + Escape(triple.Second.ToString()) + "," + triple.Third;
        });
    }

    /// <summary>
    ///     The rendering of a null draw. Spelled with the escape prefix so no rendered value can produce it: the
    ///     first thing <see cref="Escape" /> does to a backslash is double it.
    /// </summary>
    private const string NullMarker = "\\null";

    /// <summary>Renders a case as one reference line: the key, the draws it consumed, and what it produced.</summary>
    private static string Line(string name, long draws, string value) {
        return name + " | draws=" + Render(draws) + " | " + value;
    }

    private static string Render(long value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Renders a floating-point draw as its raw bits, never through a formatter. Two reasons, and the first is
    ///     load-bearing: <c>ToString("R")</c> does not produce the same text on .NET Framework as on modern .NET,
    ///     and this suite runs on both, so a formatted reference would go red on the net472 floor over a rendering
    ///     difference while the mapping was identical. The second is that a formatter rounds, so it can hide a
    ///     mapping change too small to survive its output — the opposite failure, and the silent one. Both legs are
    ///     little-endian x86-64, so the byte order this reinterpretation depends on is the same on each.
    /// </summary>
    private static string Bits(double value) {
        return BitConverter.DoubleToInt64Bits(value).ToString("x16", CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     The <see cref="float" /> counterpart of <see cref="Bits(double)" />. Goes through
    ///     <see cref="BitConverter.GetBytes(float)" /> rather than <c>SingleToInt32Bits</c>, which the net472 floor
    ///     does not carry.
    /// </summary>
    private static string Bits(float value) {
        return BitConverter.ToInt32(BitConverter.GetBytes(value), 0).ToString("x8", CultureInfo.InvariantCulture);
    }

    private static string Join(IEnumerable<string> values) {
        return string.Join(",", values);
    }

    /// <summary>Renders an UNORDERED container: the values it holds, in a fixed order this suite imposes.</summary>
    private static string JoinSorted(IEnumerable<string> values) {
        return string.Join(",", values.OrderBy(value => value, StringComparer.Ordinal));
    }

    /// <summary>
    ///     Renders a drawn string so one reference line stays one line and survives a round trip through the file:
    ///     a backslash is doubled, the field separator is escaped, and anything non-printable becomes \uXXXX.
    ///     Everything else is written as itself, so a human reading the reference still sees the value.
    /// </summary>
    private static string Escape(string value) {
        StringBuilder rendered = new(value.Length);
        foreach (char character in value) {
            if (character is '\\' or '|') {
                rendered.Append('\\').Append(character);
            } else if (character < ' ' || character == '\u007f' || char.IsSurrogate(character)) {
                rendered.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
            } else {
                rendered.Append(character);
            }
        }

        return rendered.ToString();
    }

    /// <summary>
    ///     The reference file, read from the test assembly's directory. Copied to the output rather than embedded,
    ///     so the file a failing run names is the file in the repository. Blank and #-prefixed lines are the file's
    ///     header and are not cases.
    /// </summary>
    private static string[] ReadReference() {
        string directory = Path.GetDirectoryName(typeof(SeedGoldenMasterTests).GetTypeInfo().Assembly.Location)!;
        string path      = Path.Combine(directory, ReferenceFileName);

        Check.That(File.Exists(path)).As($"the reference file '{path}' next to the test assembly").IsTrue();

        return File.ReadAllLines(path)
                   .Where(line => line.Length > 0 && line[0] != '#')
                   .ToArray();
    }

    /// <summary>Draws every case, each in its own fresh seed scope, and renders the run as reference lines.</summary>
    private static string[] DrawAll() {
        List<string> lines = [];
        foreach ((string Name, Func<string> Draw) item in Cases()) {
            using (Dummy.UseSeed(GoldenSeed)) {
                string value = item.Draw();

                lines.Add(Line(item.Name, AmbientDraws(), value));
            }
        }

        return lines.ToArray();
    }

    /// <summary>
    ///     How many draws the scope currently in force has taken. Read inside the scope, since leaving it restores
    ///     whatever source was pinned before and the count would then belong to that one.
    /// </summary>
    private static long AmbientDraws() {
        return AmbientRandomSource.Instance.Current.Draws;
    }

    private static string Key(string line) {
        int separator = line.IndexOf('|');

        return separator < 0 ? line.Trim() : line.Substring(0, separator).Trim();
    }

    #endregion

    [Fact(DisplayName = "Every factory draws the value and consumes the draws the reference file pins (ADR-0049).")]
    public void TheSeedMappingMatchesTheReference() {
        string[] actual   = DrawAll();
        string[] expected = ReadReference();

        // Compared key by key rather than as two blocks: a whole-file equality would report "the file changed" and
        // leave the reader to diff it by eye, when the useful fact is WHICH factory moved and how.
        Dictionary<string, string> expectedByKey = expected.ToDictionary(Key, line => line, StringComparer.Ordinal);
        List<string>               differences   = [];

        foreach (string line in actual) {
            string key = Key(line);
            if (!expectedByKey.TryGetValue(key, out string? reference)) {
                // The drawn line is printed so a NEW factory can be pinned by copying it. That affordance is
                // deliberately limited to this branch: a factory with no reference line is unpinned, and copying
                // its first line pins it. A factory whose line MOVED is a different situation entirely, and its
                // branch below prints both lines so the reader can see what changed — not so they can paste it.
                differences.Add($"  '{key}' has no reference line, so nothing pins it. Add this line to {ReferenceFileName}, in a commit that says a factory was added:{Environment.NewLine}      {line}");
            } else if (!string.Equals(reference, line, StringComparison.Ordinal)) {
                differences.Add($"  '{key}' moved.{Environment.NewLine}      reference: {reference}{Environment.NewLine}      actual:    {line}");
            }
        }

        HashSet<string> drawn = new(actual.Select(Key), StringComparer.Ordinal);
        foreach (string key in expectedByKey.Keys.Where(key => !drawn.Contains(key)).OrderBy(key => key, StringComparer.Ordinal)) {
            differences.Add($"  '{key}' is pinned in the reference but was not drawn. Removing a factory is a public-surface change; if it was deliberate, remove its reference line in the same commit.");
        }

        Check.That(differences)
             .As($"the seed mapping under seed {GoldenSeed}, which ADR-0049 makes a MAJOR-version change — do NOT refresh {ReferenceFileName} to make this green:{Environment.NewLine}{string.Join(Environment.NewLine, differences)}{Environment.NewLine}")
             .IsEmpty();
    }

    [Fact(DisplayName = "The reference file pins every case exactly once.")]
    public void TheReferenceFilePinsEveryCaseOnce() {
        // A duplicated key would let the lookup above silently keep one line and never compare the other, so the
        // guarantee would quietly cover less than the file appears to say it does.
        string[] duplicates = ReadReference()
                              .GroupBy(Key, StringComparer.Ordinal)
                              .Where(group => group.Count() > 1)
                              .Select(group => group.Key)
                              .ToArray();

        Check.That(duplicates).As("keys appearing more than once in the reference file").IsEmpty();
    }

    [Fact(DisplayName = "Two runs of the same seed draw the same lines, so the reference pins a mapping and not noise.")]
    public void TheMappingIsStableWithinARun() {
        // Guards the golden master itself: a case that leaked ambient state, or drew from something other than its
        // own scope, would make the reference file fail intermittently rather than on a real change — and an
        // intermittent golden master is one that gets refreshed on sight, which is exactly what must not happen here.
        Check.That(DrawAll()).IsEqualTo(DrawAll());
    }

    #region Nested types

    /// <summary>A closed set of members for the enum case, owned here so the pinned draw cannot move under it.</summary>
    private enum GoldenSuit {

        Clubs,
        Diamonds,
        Hearts,
        Spades

    }

    #endregion

}
