#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The value identity of <see cref="PoolRejection{T}" />: a rejection is a fact about a generator — this supplied
///     value, refused by these declared constraints — so two rejections stating the same fact are the same one. What
///     <see cref="ValueObjectConventionTests" /> settles by reflection is that the members exist; that two equal
///     rejections really hash alike, and what "the same value" means when the pooled type is opaque, is settled here.
/// </summary>
/// <remarks>
///     These belong to the example suite (ADR-0019): each asserts one named pair, and the null cases have no input
///     space.
/// </remarks>
[TestSubject(typeof(PoolRejection<>))]
public sealed class PoolRejectionTests {

    private static readonly DeclaredConstraint WithLength3 = new("WithLength", "3");
    private static readonly DeclaredConstraint Numeric     = new("Numeric", "");

    [Fact(DisplayName = "A rejection renders the value, then what refuses it.")]
    public void RendersTheValueThenItsCulprits() {
        PoolRejection<string> rejection = new("de", [WithLength3, Numeric]);

        Check.That(rejection.ToString()).IsEqualTo("de rejected by WithLength(3), Numeric()");
    }

    [Fact(DisplayName = "Two rejections built apart but stating the same fact are equal and hash alike.")]
    public void EqualsAnotherRejectionStatingTheSameFact() {
        PoolRejection<string> first  = new("de", [WithLength3]);
        PoolRejection<string> second = new("de", [new DeclaredConstraint("WithLength", "3")]);

        Check.That(first.Equals(second)).IsTrue();
        Check.That(first == second).IsTrue();
        Check.That(first != second).IsFalse();
        Check.That(first.GetHashCode()).IsEqualTo(second.GetHashCode());
    }

    [Fact(DisplayName = "The same value refused by other constraints is another rejection.")]
    public void DiffersFromTheSameValueWithOtherCulprits() {
        PoolRejection<string> first  = new("de", [WithLength3]);
        PoolRejection<string> second = new("de", [WithLength3, Numeric]);

        Check.That(first.Equals(second)).IsFalse();
        Check.That(first != second).IsTrue();
    }

    [Fact(DisplayName = "Another value refused by the same constraints is another rejection.")]
    public void DiffersFromAnotherValue() {
        PoolRejection<string> first  = new("de", [WithLength3]);
        PoolRejection<string> second = new("fg", [WithLength3]);

        Check.That(first == second).IsFalse();
    }

    // The order is the specification's own, and it is stable for a given generator — so two reports of the same
    // generator compare equal, while a different order is a different report rather than silently the same one.
    [Fact(DisplayName = "The constraints are compared in order.")]
    public void ComparesTheCulpritsInOrder() {
        PoolRejection<string> first  = new("de", [WithLength3, Numeric]);
        PoolRejection<string> second = new("de", [Numeric, WithLength3]);

        Check.That(first == second).IsFalse();
    }

    // The equality is the pooled type's, componentwise: for a type carrying value equality, "the same value" means
    // the same value; for one carrying none, it means the same instance. Both readings are the right one for a
    // report about a caller's own catalogue -- two distinct entities are two entries there, and rejecting one says
    // nothing about the other.
    [Fact(DisplayName = "Equality over the value is the pooled type's own, so an identity type compares by instance.")]
    public void DelegatesEqualityOverTheValueToThePooledType() {
        Entity first  = new("A");
        Entity second = new("A");

        Check.That(new PoolRejection<Entity>(first, [WithLength3]) == new PoolRejection<Entity>(first, [WithLength3])).IsTrue();
        Check.That(new PoolRejection<Entity>(first, [WithLength3]) == new PoolRejection<Entity>(second, [WithLength3])).IsFalse();
    }

    [Fact(DisplayName = "A rejection equals neither null nor a value of another type.")]
    public void EqualsNeitherNullNorAnotherType() {
        PoolRejection<string>  rejection = new("de", [WithLength3]);
        PoolRejection<string>? nothing   = null;
        object                 text      = "de rejected by WithLength(3)";

        Check.That(rejection.Equals(nothing)).IsFalse();
        Check.That(rejection.Equals(text)).IsFalse();
        Check.That(rejection == nothing).IsFalse();
        Check.That(rejection != nothing).IsTrue();
    }

    // A report a caller can watch change is not a value at all. The constructor copies, so the list handed in stays
    // the caller's business and the rejection keeps stating the fact it was minted with.
    [Fact(DisplayName = "A rejection does not follow the list it was built from.")]
    public void DoesNotFollowTheListItWasBuiltFrom() {
        List<DeclaredConstraint> culprits  = [WithLength3];
        PoolRejection<string>    rejection = new("de", culprits);

        culprits.Add(Numeric);

        Check.That(rejection.RejectedBy.Count).IsEqualTo(1);
    }

    #region Nested types

    // A reference type with no value equality — the ordinary shape of a domain entity, and the case that makes the
    // delegation above observable.
    private sealed class Entity {

        private readonly string _name;

        public Entity(string name) {
            _name = name;
        }

        public override string ToString() {
            return _name;
        }

    }

    #endregion

}
