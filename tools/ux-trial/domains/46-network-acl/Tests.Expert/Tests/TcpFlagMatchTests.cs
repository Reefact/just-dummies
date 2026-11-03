using JustDummies;

using NetworkAcl.Domain;
using NetworkAcl.Tests.Expert.Arbitrary;

using Xunit;

namespace NetworkAcl.Tests.Expert.Tests;

public sealed class TcpFlagMatchTests {

    private const TcpFlags AllFlags = (TcpFlags)0xFF;

    [Fact]
    public void A_test_exposes_its_mask_and_expected_flags() {
        // Act
        TcpFlagMatch match = new TcpFlagMatch(TcpFlags.Syn | TcpFlags.Ack | TcpFlags.Fin, TcpFlags.Ack);

        // Assert
        Assert.Equal(TcpFlags.Syn | TcpFlags.Ack | TcpFlags.Fin, match.Mask);
        Assert.Equal(TcpFlags.Ack, match.Expected);
    }

    [Fact]
    public void A_test_that_examines_no_flag_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TcpFlagMatch(TcpFlags.None, TcpFlags.None));

        // Assert
        Assert.Equal("mask", refusal.ParamName);
    }

    [Theory]
    [InlineData(TcpFlags.Syn, TcpFlags.Ack)]                         // expected entirely outside the mask
    [InlineData(TcpFlags.Syn, TcpFlags.Syn | TcpFlags.Ack)]          // expected partly outside the mask
    [InlineData(TcpFlags.Ack | TcpFlags.Psh, TcpFlags.Urg)]
    public void The_expected_flags_must_all_be_under_the_mask(TcpFlags mask, TcpFlags expected) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TcpFlagMatch(mask, expected));

        // Assert
        Assert.Equal("expected", refusal.ParamName);
    }

    [Theory]
    [InlineData(TcpFlags.Syn | TcpFlags.Fin)]
    [InlineData(TcpFlags.Syn | TcpFlags.Rst)]
    [InlineData(TcpFlags.Fin | TcpFlags.Rst)]
    [InlineData(TcpFlags.Syn | TcpFlags.Fin | TcpFlags.Ack)]   // a forbidden pair with company is still forbidden
    [InlineData(AllFlags)]
    public void Expected_flags_holding_a_forbidden_pair_are_refused_even_with_the_mask_wide_open(TcpFlags expected) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TcpFlagMatch(AllFlags, expected));

        // Assert
        Assert.Equal("expected", refusal.ParamName);
    }

    [Fact]
    public void Examining_all_eight_flags_for_none_is_refused_since_only_a_flagless_segment_would_match() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TcpFlagMatch(AllFlags, TcpFlags.None));

        // Assert
        Assert.Equal("expected", refusal.ParamName);
    }

    [Theory]
    [InlineData(TcpFlags.Syn, TcpFlags.None)]                              // "SYN clear"
    [InlineData(AllFlags & ~TcpFlags.Cwr, TcpFlags.None)]                  // seven flags examined, none set: still possible with CWR alone
    [InlineData(TcpFlags.Syn | TcpFlags.Fin | TcpFlags.Rst, TcpFlags.Syn)] // a mask may span a forbidden pair; only expected may not
    [InlineData(AllFlags, TcpFlags.Syn | TcpFlags.Ack)]
    public void Accepted_tests_are_the_ones_some_valid_segment_can_satisfy(TcpFlags mask, TcpFlags expected) {
        // Act
        TcpFlagMatch match = new TcpFlagMatch(mask, expected);

        // Assert
        Assert.Equal(expected, match.Expected);
    }

    [Fact]
    public void NewConnection_examines_syn_and_ack_and_wants_syn_alone() {
        Assert.Equal(TcpFlags.Syn | TcpFlags.Ack, TcpFlagMatch.NewConnection.Mask);
        Assert.Equal(TcpFlags.Syn, TcpFlagMatch.NewConnection.Expected);
    }

    [Fact]
    public void Established_examines_ack_and_wants_it_set() {
        Assert.Equal(TcpFlags.Ack, TcpFlagMatch.Established.Mask);
        Assert.Equal(TcpFlags.Ack, TcpFlagMatch.Established.Expected);
    }

    [Theory]
    [InlineData(TcpFlags.None, false)]
    [InlineData(TcpFlags.Syn, true)]
    [InlineData(TcpFlags.Syn | TcpFlags.Ack, true)]
    [InlineData(TcpFlags.Fin | TcpFlags.Ack, true)]
    [InlineData(TcpFlags.Rst | TcpFlags.Ack, true)]
    [InlineData(TcpFlags.Psh | TcpFlags.Urg | TcpFlags.Ack | TcpFlags.Ece | TcpFlags.Cwr, true)]
    [InlineData(TcpFlags.Syn | TcpFlags.Fin, false)]
    [InlineData(TcpFlags.Syn | TcpFlags.Rst, false)]
    [InlineData(TcpFlags.Fin | TcpFlags.Rst, false)]
    [InlineData(TcpFlags.Syn | TcpFlags.Fin | TcpFlags.Rst, false)]
    [InlineData(TcpFlags.Syn | TcpFlags.Fin | TcpFlags.Ack, false)]
    [InlineData(AllFlags, false)]
    public void IsValidCombination_rejects_no_flag_and_the_three_forbidden_pairs(TcpFlags flags, bool expected) {
        Assert.Equal(expected, TcpFlagMatch.IsValidCombination(flags));
    }

    [Theory]
    [InlineData(TcpFlags.Syn, true)]
    [InlineData(TcpFlags.Syn | TcpFlags.Ack, false)]   // ACK is examined and must be clear
    [InlineData(TcpFlags.Ack, false)]
    [InlineData(TcpFlags.None, false)]
    public void NewConnection_matches_syn_with_ack_clear(TcpFlags flags, bool expected) {
        Assert.Equal(expected, TcpFlagMatch.NewConnection.Matches(flags));
    }

    [Theory]
    [InlineData(TcpFlags.Ack, true)]
    [InlineData(TcpFlags.Syn | TcpFlags.Ack, true)]
    [InlineData(TcpFlags.Fin | TcpFlags.Ack, true)]
    [InlineData(TcpFlags.Syn, false)]
    [InlineData(TcpFlags.Rst, false)]
    public void Established_matches_any_segment_with_ack_set(TcpFlags flags, bool expected) {
        Assert.Equal(expected, TcpFlagMatch.Established.Matches(flags));
    }

    [Fact]
    public void Flags_outside_the_mask_are_ignored() {
        // Arrange
        // Whatever auxiliary flags ride along, only SYN and ACK are examined.
        TcpFlags anyAuxiliary = AnyTcpSegmentFlags.Auxiliary().Generate();

        // Act & Assert
        Assert.True(TcpFlagMatch.NewConnection.Matches(TcpFlags.Syn | anyAuxiliary));
        Assert.True(TcpFlagMatch.Established.Matches(TcpFlags.Ack | anyAuxiliary));
        Assert.False(TcpFlagMatch.NewConnection.Matches(TcpFlags.Syn | TcpFlags.Ack | anyAuxiliary));
    }

    [Theory]
    [InlineData(TcpFlags.Syn, TcpFlags.Syn, TcpFlags.Syn | TcpFlags.Ack, TcpFlags.Syn, true)]    // examines less and agrees
    [InlineData(TcpFlags.Syn | TcpFlags.Ack, TcpFlags.Syn, TcpFlags.Syn, TcpFlags.Syn, false)]   // examines more than the other
    [InlineData(TcpFlags.Syn | TcpFlags.Ack, TcpFlags.Syn, TcpFlags.Syn | TcpFlags.Ack, TcpFlags.Syn, true)]   // itself
    [InlineData(TcpFlags.Syn, TcpFlags.Syn, TcpFlags.Syn | TcpFlags.Ack, TcpFlags.Syn | TcpFlags.Ack, true)]    // SYN set is implied by SYN+ACK set
    [InlineData(TcpFlags.Ack, TcpFlags.None, TcpFlags.Syn | TcpFlags.Ack, TcpFlags.Syn, true)]                  // ACK clear is implied by NewConnection
    [InlineData(TcpFlags.Ack, TcpFlags.Ack, TcpFlags.Syn | TcpFlags.Ack, TcpFlags.Syn, false)]                  // ... and contradicted by ACK set
    [InlineData(TcpFlags.Fin, TcpFlags.Fin, TcpFlags.Ack, TcpFlags.Ack, false)]                                 // examines a flag the other ignores
    public void Covers_when_it_examines_no_more_flags_than_the_other_and_agrees_on_them(TcpFlags mask, TcpFlags expected, TcpFlags otherMask, TcpFlags otherExpected, bool coversOther) {
        // Arrange
        TcpFlagMatch match = new TcpFlagMatch(mask, expected);
        TcpFlagMatch other = new TcpFlagMatch(otherMask, otherExpected);

        // Act & Assert
        Assert.Equal(coversOther, match.Covers(other));
    }

    [Theory]
    [InlineData(TcpFlags.Syn, TcpFlags.Syn, TcpFlags.Ack, TcpFlags.Ack, true)]                          // different flags examined: they can both hold
    [InlineData(TcpFlags.Syn | TcpFlags.Ack, TcpFlags.Syn, TcpFlags.Ack, TcpFlags.Ack, false)]          // NewConnection vs Established disagree on ACK
    [InlineData(TcpFlags.Syn | TcpFlags.Ack, TcpFlags.Syn, TcpFlags.Syn, TcpFlags.None, false)]         // SYN set vs SYN clear
    [InlineData(TcpFlags.Syn | TcpFlags.Ack, TcpFlags.Syn, TcpFlags.Syn | TcpFlags.Ack, TcpFlags.Syn, true)]
    [InlineData(TcpFlags.Syn | TcpFlags.Ack, TcpFlags.Syn, TcpFlags.Ack | TcpFlags.Fin, TcpFlags.None, true)]   // agree on the one flag both examine
    public void Overlaps_when_they_agree_on_every_flag_both_examine(TcpFlags mask, TcpFlags expected, TcpFlags otherMask, TcpFlags otherExpected, bool overlap) {
        // Arrange
        TcpFlagMatch match = new TcpFlagMatch(mask, expected);
        TcpFlagMatch other = new TcpFlagMatch(otherMask, otherExpected);

        // Act & Assert: symmetric
        Assert.Equal(overlap, match.Overlaps(other));
        Assert.Equal(overlap, other.Overlaps(match));
    }

    [Fact]
    public void Covers_and_Overlaps_refuse_a_missing_test() {
        Assert.Throws<ArgumentNullException>(() => TcpFlagMatch.Established.Covers(null!));
        Assert.Throws<ArgumentNullException>(() => TcpFlagMatch.Established.Overlaps(null!));
    }

    [Fact]
    public void Tests_with_the_same_mask_and_expected_are_equal_and_hash_alike() {
        // Arrange
        TcpFlagMatch first  = new TcpFlagMatch(TcpFlags.Syn | TcpFlags.Ack, TcpFlags.Syn);
        TcpFlagMatch second = new TcpFlagMatch(TcpFlags.Syn | TcpFlags.Ack, TcpFlags.Syn);

        // Act & Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Equal(TcpFlagMatch.NewConnection, first);
        Assert.NotEqual(TcpFlagMatch.Established, first);
        Assert.False(first.Equals(null));
    }

    [Fact]
    public void An_arbitrary_test_is_always_one_the_constructor_accepts() {
        // Arrange: the generator draws through the real constructor, so a refusal here would surface as an exception.
        IAny<TcpFlagMatch> generator = Any.TcpFlagMatch();

        // Act
        TcpFlagMatch anyMatch = generator.Generate();

        // Assert
        Assert.NotEqual(TcpFlags.None, anyMatch.Mask);
    }

}
