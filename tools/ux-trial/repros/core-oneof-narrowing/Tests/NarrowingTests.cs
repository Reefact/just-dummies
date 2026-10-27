using JustDummies;
using Xunit;
using Zone.Domain;

namespace Zone.Tests;

public sealed class NarrowingTests {

    [Fact]
    public void Variant_A_lambda_subset_OneOf_on_string_base() {
        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(() => new AnyTolerance().WithZone(z => z.OneOf("H", "JS")));
        Assert.Equal("Cannot apply OneOf(\"H\", \"JS\") because OneOf(\"H\", \"h\", \"JS\", \"js\") is already defined.", refusal.Message);
    }

    [Fact]
    public void Variant_B_lambda_Except_narrows() {
        AnyTolerance holes = new AnyTolerance().WithZone(z => z.Except("h", "js"));
        for (int i = 0; i < 200; i++) {
            Tolerance anyHole = holes.Generate();
            Assert.Contains(anyHole.Zone, new[] { "H", "JS" });
        }
    }

    [Fact]
    public void Variant_C_int_subset_OneOf_is_refused_too() {
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.Int32().OneOf(1, 2, 3).OneOf(1, 2));
    }

    [Fact]
    public void Variant_D_bound_after_OneOf_intersects() {
        AnyInt32 narrowed = Any.Int32().OneOf(1, 2, 3).LessThanOrEqualTo(2);
        for (int i = 0; i < 200; i++) {
            Assert.InRange(narrowed.Generate(), 1, 2);
        }
    }

    [Fact]
    public void Variant_E_same_set_reordered_is_accepted() {
        AnyString same = Any.String().OneOf("H", "h", "JS", "js").OneOf("js", "JS", "h", "H");
        Assert.Contains(same.Generate(), new[] { "H", "h", "JS", "js" });
    }

    [Fact]
    public void Variant_F_disjoint_OneOf_message() {
        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(() => Any.String().OneOf("H", "h").OneOf("X"));
        Assert.Equal("Cannot apply OneOf(\"X\") because OneOf(\"H\", \"h\") is already defined.", refusal.Message);
    }

}
