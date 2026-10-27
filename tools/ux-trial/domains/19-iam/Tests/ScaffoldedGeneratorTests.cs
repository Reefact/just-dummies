using Identity.Domain;
using JustDummies;
using Xunit;

namespace Identity.Tests;

/// <summary>Harness: each scaffolded generator, as resolved by hand, must never trip its type's constructor.</summary>
public sealed class ScaffoldedGeneratorTests {

    private const int Draws = 500;

    private static void DrawRepeatedly<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            generator.Generate();
        }
    }

    [Fact]
    public void Scaffolded_AnyClientId_always_builds_a_valid_ClientId() => DrawRepeatedly(new AnyClientId());

    [Fact]
    public void Scaffolded_AnyScope_always_builds_a_valid_Scope() => DrawRepeatedly(new AnyScope());

    [Fact]
    public void Scaffolded_AnyRedirectUri_always_builds_a_valid_RedirectUri() => DrawRepeatedly(new AnyRedirectUri());

    [Fact]
    public void Scaffolded_AnyTokenPolicy_always_builds_a_valid_TokenPolicy() => DrawRepeatedly(Any.TokenPolicy());

    [Fact]
    public void Scaffolded_AnyRoleAssignment_always_builds_a_valid_RoleAssignment() => DrawRepeatedly(Arbitrary.RoleAssignment());

    [Fact]
    public void Scaffolded_AnyClientApplication_always_builds_a_valid_ClientApplication() => DrawRepeatedly(new AnyClientApplication());

    [Fact]
    public void Scaffolded_AnyAccessToken_always_builds_a_valid_AccessToken() => DrawRepeatedly(new AnyAccessToken());

    [Fact]
    public void Scaffolded_AnyTokenIssuer_always_builds_a_valid_TokenIssuer() => DrawRepeatedly(new AnyTokenIssuer());

    [Fact]
    public void Scaffolded_AnyPostLogoutRedirects_always_builds_a_valid_PostLogoutRedirects() => DrawRepeatedly(new AnyPostLogoutRedirects());

    // `any generate RoleSet` reported "1 of 1 parameters inferred" with no "to verify", yet RoleSet
    // refuses a duplicated role through a check on the field it copied the parameter into.
    [Fact]
    public void Bug_Scaffolded_AnyRoleSet_ignores_a_distinctness_guard_written_against_a_field() => DrawRepeatedly(new AnyRoleSet());

    // `any generate Role` scaffolded `new Role()` for an enum: every "arbitrary" Role is Role.Reader.
    [Fact]
    public void Bug_Scaffolded_AnyRole_for_an_enum_only_ever_draws_the_default_member() {
        HashSet<Role> drawn = new HashSet<Role>();
        for (int draw = 0; draw < 200; draw++) {
            drawn.Add(new AnyRole().Generate());
        }

        Assert.True(drawn.Count > 1, $"200 draws of AnyRole only produced: {string.Join(", ", drawn)}");
    }

}
