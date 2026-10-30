using Identity.Domain;
using JustDummies;
using Xunit;

namespace Identity.Tests;

/// <summary>Chains an analyzer should catch at build time, written as ordinary statements (not lambda bodies).</summary>
public sealed class AnalyzerProbes {

    [Fact]
    public void Five_distinct_roles_out_of_four_is_refused() {
        HashSet<Role>? roles = null;
        try {
            roles = Any.SetOf(Any.Enum<Role>()).WithCount(5).Generate();
        } catch (ConflictingAnyConstraintException) {
        }

        Assert.Null(roles);
    }

    [Fact]
    public void Two_schemes_on_one_web_uri_are_refused() {
        Uri? uri = null;
        try {
            uri = Any.Uri().Web().UsingHttps().UsingHttp().Generate();
        } catch (ConflictingAnyConstraintException) {
        }

        Assert.Null(uri);
    }

    [Fact]
    public void A_fixed_offset_and_an_offset_range_are_refused() {
        DateTimeOffset? issuedAt = null;
        try {
            issuedAt = Any.DateTimeOffset().WithOffset(TimeSpan.FromHours(5)).WithOffsetBetween(TimeSpan.FromHours(0), TimeSpan.FromHours(3)).Generate();
        } catch (ConflictingAnyConstraintException) {
        }

        Assert.Null(issuedAt);
    }

    [Fact]
    public void An_empty_and_a_non_empty_guid_are_refused() {
        Guid? id = null;
        try {
            id = Any.Guid().Empty().NonEmpty().Generate();
        } catch (ConflictingAnyConstraintException) {
        }

        Assert.Null(id);
    }

    [Fact]
    public void An_empty_relative_reference_is_refused_at_generate() {
        Uri? uri = null;
        try {
            uri = Any.Uri().Relative().WithPathSegments(0).Generate();
        } catch (AnyGenerationException) {
        }

        Assert.Null(uri);
    }

    // A drawn value reaching the assertion: the test is a property with a sample size of one.
    [Fact]
    public void A_drawn_scope_flowing_into_the_assertion() {
        string anyScope = Any.StringMatching("^[a-z]{3,8}:(read|write)$").Generate();

        Scope scope = new Scope(anyScope);

        Assert.Equal(anyScope, scope.Value);
    }

}
