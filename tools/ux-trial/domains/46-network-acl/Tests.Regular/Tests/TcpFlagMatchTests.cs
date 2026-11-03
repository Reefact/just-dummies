using JustDummies;

using NetworkAcl.Domain;

using Xunit;

namespace NetworkAcl.Tests.Regular;

public sealed class TcpFlagMatchTests {

    private const TcpFlags AllFlags = (TcpFlags)0xFF;

    [Fact]
    public void A_test_that_examines_no_flag_is_refused() {
        // Arrange
        TcpFlags anyExpected = Any.Enum<TcpFlags>().Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new TcpFlagMatch(TcpFlags.None, anyExpected));

        // Assert
        Assert.Equal("mask", thrown.ParamName);
    }

    [Theory]
    [InlineData(TcpFlags.Syn, TcpFlags.Ack)]
    [InlineData(TcpFlags.Syn | TcpFlags.Ack, TcpFlags.Syn | TcpFlags.Rst)]
    [InlineData(TcpFlags.Fin, TcpFlags.Fin | TcpFlags.Ack)]
    public void An_expected_flag_outside_the_mask_is_refused(TcpFlags mask, TcpFlags expected) {
        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new TcpFlagMatch(mask, expected));

        // Assert
        Assert.Equal("expected", thrown.ParamName);
    }

    [Theory]
    [InlineData(TcpFlags.Syn | TcpFlags.Fin)]
    [InlineData(TcpFlags.Syn | TcpFlags.Rst)]
    [InlineData(TcpFlags.Fin | TcpFlags.Rst)]
    [InlineData(TcpFlags.Syn | TcpFlags.Fin | TcpFlags.Ack)]
    [InlineData(TcpFlags.Fin | TcpFlags.Rst | TcpFlags.Psh)]
    [InlineData(TcpFlags.Syn | TcpFlags.Fin | TcpFlags.Rst)]
    public void An_expected_set_no_segment_can_carry_is_refused(TcpFlags expected) {
        // Arrange
        TcpFlags anyExtraMaskBits = Any.Enum<TcpFlags>().AllowingCombinations().Generate();

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new TcpFlagMatch(expected | anyExtraMaskBits, expected));

        // Assert
        Assert.Equal("expected", thrown.ParamName);
    }

    [Fact]
    public void Examining_all_eight_flags_expecting_none_would_never_match() {
        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new TcpFlagMatch(AllFlags, TcpFlags.None));

        // Assert
        Assert.Equal("expected", thrown.ParamName);
    }

    [Fact]
    public void Examining_seven_flags_expecting_none_is_fine() {
        // Arrange
        TcpFlags anyFlagLeftOut = Any.Enum<TcpFlags>().DifferentFrom(TcpFlags.None).Generate();

        // Act
        TcpFlagMatch match = new TcpFlagMatch(AllFlags & ~anyFlagLeftOut, TcpFlags.None);

        // Assert
        Assert.True(match.Matches(anyFlagLeftOut));
    }

    [Fact]
    public void A_connection_opening_is_syn_set_and_ack_clear() {
        // Arrange
        TcpFlagMatch opening = TcpFlagMatch.NewConnection;

        // Assert
        Assert.Equal(TcpFlags.Syn | TcpFlags.Ack, opening.Mask);
        Assert.Equal(TcpFlags.Syn, opening.Expected);
        Assert.True(opening.Matches(TcpFlags.Syn));
        Assert.True(opening.Matches(TcpFlags.Syn | TcpFlags.Psh));
        Assert.False(opening.Matches(TcpFlags.Syn | TcpFlags.Ack));
        Assert.False(opening.Matches(TcpFlags.Ack));
        Assert.False(opening.Matches(TcpFlags.Fin));
    }

    [Fact]
    public void An_established_segment_has_ack_set() {
        // Arrange
        TcpFlagMatch established = TcpFlagMatch.Established;

        // Assert
        Assert.Equal(TcpFlags.Ack, established.Mask);
        Assert.Equal(TcpFlags.Ack, established.Expected);
        Assert.True(established.Matches(TcpFlags.Ack));
        Assert.True(established.Matches(TcpFlags.Ack | TcpFlags.Psh));
        Assert.True(established.Matches(TcpFlags.Syn | TcpFlags.Ack));
        Assert.False(established.Matches(TcpFlags.Syn));
        Assert.False(established.Matches(TcpFlags.None));
    }

    [Fact]
    public void Flags_outside_the_mask_do_not_change_the_outcome() {
        // Arrange
        TcpFlagMatch anyMatch   = Any.TcpFlagMatch().Generate();
        TcpFlags     outsideBits = (TcpFlags)Any.Byte().Generate() & ~anyMatch.Mask;

        // Assert
        Assert.True(anyMatch.Matches(anyMatch.Expected));
        Assert.True(anyMatch.Matches(anyMatch.Expected | outsideBits));
    }

    [Fact]
    public void One_wrong_flag_under_the_mask_fails_the_match() {
        // Arrange
        TcpFlagMatch anyMatch      = Any.TcpFlagMatch().Generate();
        TcpFlags     lowestUnderMask = (TcpFlags)((byte)anyMatch.Mask & -(byte)anyMatch.Mask);
        TcpFlags     outsideBits   = (TcpFlags)Any.Byte().Generate() & ~anyMatch.Mask;

        // Act
        bool matches = anyMatch.Matches((anyMatch.Expected ^ lowestUnderMask) | outsideBits);

        // Assert
        Assert.False(matches);
    }

    [Fact]
    public void Of_the_256_flag_sets_exactly_the_empty_one_and_those_with_a_contradictory_pair_are_invalid() {
        // Arrange
        int valid = 0;

        // Act
        for (int bits = 0; bits < 256; bits++) {
            bool hasPair = (bits & 0x03) == 0x03   // SYN + FIN
                        || (bits & 0x06) == 0x06   // SYN + RST
                        || (bits & 0x05) == 0x05;  // FIN + RST
            bool expectedValid = bits != 0 && !hasPair;

            Assert.Equal(expectedValid, TcpFlagMatch.IsValidCombination((TcpFlags)bits));

            if (expectedValid) { valid++; }
        }

        // Assert: the 32 combinations of the five unconstrained flags times the 4 admissible states of FIN/SYN/RST
        // (none, one of them alone), less the empty set.
        Assert.Equal(32 * 4 - 1, valid);
    }

    [Theory]
    [InlineData(TcpFlags.Syn, true)]
    [InlineData(TcpFlags.Fin, true)]
    [InlineData(TcpFlags.Rst, true)]
    [InlineData(TcpFlags.Syn | TcpFlags.Ack, true)]
    [InlineData(TcpFlags.Fin | TcpFlags.Ack, true)]
    [InlineData(TcpFlags.Rst | TcpFlags.Ack, true)]
    [InlineData(TcpFlags.Psh | TcpFlags.Urg | TcpFlags.Ack, true)]
    [InlineData(TcpFlags.Ece | TcpFlags.Cwr | TcpFlags.Syn | TcpFlags.Ack, true)]
    [InlineData(TcpFlags.None, false)]
    [InlineData(TcpFlags.Syn | TcpFlags.Fin, false)]
    [InlineData(TcpFlags.Syn | TcpFlags.Rst, false)]
    [InlineData(TcpFlags.Fin | TcpFlags.Rst, false)]
    [InlineData((TcpFlags)0xFF, false)]
    public void Valid_combinations_are_non_empty_and_free_of_contradictory_pairs(TcpFlags flags, bool expected) {
        // Act & Assert
        Assert.Equal(expected, TcpFlagMatch.IsValidCombination(flags));
    }

    [Fact]
    public void A_test_covers_itself_and_overlaps_itself() {
        // Arrange
        TcpFlagMatch anyMatch = Any.TcpFlagMatch().Generate();

        // Assert
        Assert.True(anyMatch.Covers(anyMatch));
        Assert.True(anyMatch.Overlaps(anyMatch));
    }

    [Fact]
    public void A_test_examining_fewer_flags_covers_one_that_agrees_and_examines_more() {
        // Arrange
        TcpFlagMatch established = TcpFlagMatch.Established;
        TcpFlagMatch ackAndPsh   = new TcpFlagMatch(TcpFlags.Ack | TcpFlags.Psh, TcpFlags.Ack | TcpFlags.Psh);

        // Assert
        Assert.True(established.Covers(ackAndPsh));
        Assert.False(ackAndPsh.Covers(established));
    }

    [Fact]
    public void A_test_does_not_cover_one_that_expects_otherwise() {
        // Arrange
        TcpFlagMatch established = TcpFlagMatch.Established;
        TcpFlagMatch opening     = TcpFlagMatch.NewConnection;

        // Assert
        Assert.False(established.Covers(opening));
        Assert.False(opening.Covers(established));
    }

    [Fact]
    public void Tests_that_disagree_on_a_flag_both_examine_cannot_overlap() {
        // Arrange
        TcpFlagMatch established = TcpFlagMatch.Established;
        TcpFlagMatch opening     = TcpFlagMatch.NewConnection;

        // Assert
        Assert.False(established.Overlaps(opening));
        Assert.False(opening.Overlaps(established));
    }

    [Fact]
    public void Tests_that_examine_different_flags_overlap() {
        // Arrange
        TcpFlagMatch established = TcpFlagMatch.Established;
        TcpFlagMatch push        = new TcpFlagMatch(TcpFlags.Psh, TcpFlags.Psh);

        // Assert
        Assert.True(established.Overlaps(push));
        Assert.True(push.Overlaps(established));
        Assert.False(established.Covers(push));
    }

    [Fact]
    public void Covers_and_Overlaps_refuse_null() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TcpFlagMatch.Established.Covers(null!));
        Assert.Throws<ArgumentNullException>(() => TcpFlagMatch.Established.Overlaps(null!));
    }

    [Fact]
    public void Tests_are_equal_by_mask_and_expected() {
        // Arrange
        TcpFlagMatch anyMatch = Any.TcpFlagMatch().Generate();
        TcpFlagMatch copy     = new TcpFlagMatch(anyMatch.Mask, anyMatch.Expected);

        // Assert
        Assert.Equal(anyMatch, copy);
        Assert.Equal(anyMatch.GetHashCode(), copy.GetHashCode());
        Assert.False(anyMatch.Equals(null));
    }

}
