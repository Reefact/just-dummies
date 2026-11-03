using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Discovery;

public sealed class TcpFlagMatchTests {

    [Fact]
    public void A_test_examining_no_flag_is_refused() {
        Assert.Throws<ArgumentException>(() => new TcpFlagMatch(TcpFlags.None, TcpFlags.None));
    }

    [Theory]
    [InlineData(TcpFlags.Syn, TcpFlags.Ack)]
    [InlineData(TcpFlags.Syn | TcpFlags.Ack, TcpFlags.Fin)]
    [InlineData(TcpFlags.Ack, TcpFlags.Ack | TcpFlags.Psh)]
    public void An_expectation_outside_the_mask_is_refused(TcpFlags mask, TcpFlags expected) {
        Assert.Throws<ArgumentException>(() => new TcpFlagMatch(mask, expected));
    }

    [Theory]
    [InlineData(TcpFlags.Syn | TcpFlags.Fin)]
    [InlineData(TcpFlags.Syn | TcpFlags.Rst)]
    [InlineData(TcpFlags.Fin | TcpFlags.Rst)]
    [InlineData(TcpFlags.Syn | TcpFlags.Fin | TcpFlags.Rst)]
    [InlineData(TcpFlags.Syn | TcpFlags.Fin | TcpFlags.Ack)]
    public void An_expectation_no_valid_segment_carries_is_refused(TcpFlags expected) {
        Assert.Throws<ArgumentException>(() => new TcpFlagMatch((TcpFlags)0xFF, expected));
    }

    [Fact]
    public void Examining_every_flag_to_find_none_set_is_refused() {
        Assert.Throws<ArgumentException>(() => new TcpFlagMatch((TcpFlags)0xFF, TcpFlags.None));
    }

    [Fact]
    public void Examining_every_flag_but_one_to_find_none_set_is_accepted() {
        TcpFlagMatch match = new TcpFlagMatch((TcpFlags)0xFE, TcpFlags.None);

        Assert.Equal(TcpFlags.None, match.Expected);
    }

    [Fact]
    public void A_mask_of_one_flag_expecting_it_clear_is_accepted() {
        Assert.Equal(TcpFlags.Syn, new TcpFlagMatch(TcpFlags.Syn, TcpFlags.None).Mask);
    }

    [Fact]
    public void The_forbidden_pairs_are_only_refused_when_expected_so_a_mask_may_hold_them() {
        TcpFlagMatch match = new TcpFlagMatch(TcpFlags.Syn | TcpFlags.Fin, TcpFlags.Syn);

        Assert.Equal(TcpFlags.Syn | TcpFlags.Fin, match.Mask);
    }

    [Fact]
    public void NewConnection_examines_syn_and_ack_and_expects_syn() {
        Assert.Equal(TcpFlags.Syn | TcpFlags.Ack, TcpFlagMatch.NewConnection.Mask);
        Assert.Equal(TcpFlags.Syn, TcpFlagMatch.NewConnection.Expected);
    }

    [Fact]
    public void Established_examines_ack_and_expects_ack() {
        Assert.Equal(TcpFlags.Ack, TcpFlagMatch.Established.Mask);
        Assert.Equal(TcpFlags.Ack, TcpFlagMatch.Established.Expected);
    }

    [Theory]
    [InlineData(TcpFlags.None, false)]
    [InlineData(TcpFlags.Fin, true)]
    [InlineData(TcpFlags.Syn, true)]
    [InlineData(TcpFlags.Rst, true)]
    [InlineData(TcpFlags.Psh, true)]
    [InlineData(TcpFlags.Ack, true)]
    [InlineData(TcpFlags.Urg, true)]
    [InlineData(TcpFlags.Ece, true)]
    [InlineData(TcpFlags.Cwr, true)]
    [InlineData(TcpFlags.Syn | TcpFlags.Ack, true)]
    [InlineData(TcpFlags.Fin | TcpFlags.Ack, true)]
    [InlineData(TcpFlags.Rst | TcpFlags.Ack, true)]
    [InlineData(TcpFlags.Syn | TcpFlags.Ece | TcpFlags.Cwr, true)]
    [InlineData(TcpFlags.Syn | TcpFlags.Fin, false)]
    [InlineData(TcpFlags.Syn | TcpFlags.Rst, false)]
    [InlineData(TcpFlags.Fin | TcpFlags.Rst, false)]
    [InlineData(TcpFlags.Syn | TcpFlags.Fin | TcpFlags.Rst, false)]
    [InlineData(TcpFlags.Syn | TcpFlags.Fin | TcpFlags.Ack, false)]
    [InlineData((TcpFlags)0xFF, false)]
    [InlineData((TcpFlags)0xF8, true)]
    public void IsValidCombination_refuses_none_and_the_contradictory_pairs(TcpFlags flags, bool expected) {
        Assert.Equal(expected, TcpFlagMatch.IsValidCombination(flags));
    }

    [Theory]
    [InlineData(TcpFlags.Syn, true)]
    [InlineData(TcpFlags.Syn | TcpFlags.Psh, true)]
    [InlineData(TcpFlags.Syn | TcpFlags.Urg | TcpFlags.Ece, true)]
    [InlineData(TcpFlags.Syn | TcpFlags.Ack, false)]
    [InlineData(TcpFlags.Ack, false)]
    [InlineData(TcpFlags.Ack | TcpFlags.Psh, false)]
    [InlineData(TcpFlags.Fin | TcpFlags.Ack, false)]
    [InlineData(TcpFlags.Rst, false)]
    [InlineData(TcpFlags.None, false)]
    public void NewConnection_matches_syn_without_ack(TcpFlags flags, bool expected) {
        Assert.Equal(expected, TcpFlagMatch.NewConnection.Matches(flags));
    }

    [Theory]
    [InlineData(TcpFlags.Ack, true)]
    [InlineData(TcpFlags.Syn | TcpFlags.Ack, true)]
    [InlineData(TcpFlags.Fin | TcpFlags.Ack, true)]
    [InlineData(TcpFlags.Psh | TcpFlags.Ack, true)]
    [InlineData(TcpFlags.Syn, false)]
    [InlineData(TcpFlags.Rst, false)]
    public void Established_matches_any_segment_with_ack(TcpFlags flags, bool expected) {
        Assert.Equal(expected, TcpFlagMatch.Established.Matches(flags));
    }

    [Fact]
    public void A_flag_outside_the_mask_does_not_matter() {
        // Arrange: URG is not examined, so a dummy mask-compatible choice of the other flags is irrelevant
        TcpFlagMatch synOnly = new TcpFlagMatch(TcpFlags.Syn, TcpFlags.Syn);

        // Assert
        Assert.True(synOnly.Matches(TcpFlags.Syn | TcpFlags.Ack | TcpFlags.Urg));
        Assert.True(synOnly.Matches(TcpFlags.Syn));
        Assert.False(synOnly.Matches(TcpFlags.Ack | TcpFlags.Urg));
    }

    [Fact]
    public void A_test_covers_itself() {
        Assert.True(TcpFlagMatch.NewConnection.Covers(TcpFlagMatch.NewConnection));
    }

    [Fact]
    public void A_test_examining_fewer_flags_covers_one_that_agrees_and_examines_more() {
        TcpFlagMatch synOnly    = new TcpFlagMatch(TcpFlags.Syn, TcpFlags.Syn);
        TcpFlagMatch synNoAck   = TcpFlagMatch.NewConnection;

        Assert.True(synOnly.Covers(synNoAck));
        Assert.False(synNoAck.Covers(synOnly));
    }

    [Fact]
    public void A_test_does_not_cover_one_that_disagrees_on_a_shared_flag() {
        Assert.False(TcpFlagMatch.Established.Covers(TcpFlagMatch.NewConnection));
        Assert.False(TcpFlagMatch.NewConnection.Covers(TcpFlagMatch.Established));
    }

    [Fact]
    public void Tests_that_agree_on_every_shared_flag_overlap() {
        TcpFlagMatch push = new TcpFlagMatch(TcpFlags.Psh, TcpFlags.Psh);

        Assert.True(TcpFlagMatch.Established.Overlaps(push));
        Assert.True(push.Overlaps(TcpFlagMatch.Established));
        Assert.True(TcpFlagMatch.NewConnection.Overlaps(push));
    }

    [Fact]
    public void Tests_that_disagree_on_a_shared_flag_do_not_overlap() {
        Assert.False(TcpFlagMatch.Established.Overlaps(TcpFlagMatch.NewConnection));
        Assert.False(TcpFlagMatch.NewConnection.Overlaps(TcpFlagMatch.Established));
    }

    [Fact]
    public void Covers_and_overlaps_refuse_null() {
        Assert.Throws<ArgumentNullException>(() => TcpFlagMatch.Established.Covers(null!));
        Assert.Throws<ArgumentNullException>(() => TcpFlagMatch.Established.Overlaps(null!));
    }

    [Fact]
    public void Tests_are_equal_when_mask_and_expectation_are() {
        Assert.Equal(new TcpFlagMatch(TcpFlags.Syn | TcpFlags.Ack, TcpFlags.Syn), TcpFlagMatch.NewConnection);
        Assert.Equal(TcpFlagMatch.NewConnection.GetHashCode(), new TcpFlagMatch(TcpFlags.Syn | TcpFlags.Ack, TcpFlags.Syn).GetHashCode());
        Assert.NotEqual(TcpFlagMatch.NewConnection, TcpFlagMatch.Established);
    }


}
