using ParimutuelBetting.Domain.Tests.Expert.Generators;
using ParimutuelBetting.Domain.Tests.Expert.Support;

namespace ParimutuelBetting.Domain.Tests.Expert;

public sealed class StakePoolLifecycleTests {

    private const int FieldSize = 8;

    private static StakePool OpenPool(BetType betType = BetType.Win) {
        Race race = Any.Race().WithFieldSize(FieldSize).Generate();

        return Any.StakePool().WithRace(race).WithBetType(betType).WithRules(r => r.WithPlacesPaid(2)).Generate();
    }

    private static Stake AnyTicketOn(BetType betType, int units = 1) {
        return Any.Stake().WithCombination(c => c.WithBetType(betType).AmongRunnersOf(FieldSize)).WithUnits(units).Generate();
    }

    [Fact]
    public void An_open_pool_collects_the_stakes_it_accepts() {
        StakePool pool = OpenPool(BetType.Win);
        Stake     first  = AnyTicketOn(BetType.Win, units: 3);
        Stake     second = AnyTicketOn(BetType.Win, units: 4);

        pool.Accept(first);
        pool.Accept(second);

        Assert.Equal(new[] { first, second }, pool.Stakes);
    }

    [Fact]
    public void The_money_collected_is_the_total_units_times_the_unit_stake() {
        StakePool pool = Any.StakePool().WithRace(Any.Race().WithFieldSize(FieldSize).Generate()).WithBetType(BetType.Win).WithUnitStake(2.50m).Generate();

        pool.Accept(AnyTicketOn(BetType.Win, units: 3));
        pool.Accept(AnyTicketOn(BetType.Win, units: 4));

        Assert.Equal(17.50m, pool.GrossAmount);
    }

    [Fact]
    public void A_runner_of_the_race_can_be_backed_up_to_the_last_number_of_the_field() {
        StakePool pool = OpenPool(BetType.Win);

        pool.Accept(Tickets.Ticket(BetType.Win, 1, FieldSize));

        Assert.Single(pool.Stakes);
    }

    [Fact]
    public void A_horse_that_is_not_a_runner_of_the_race_cannot_be_backed() {
        StakePool pool = OpenPool(BetType.Win);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => pool.Accept(Tickets.Ticket(BetType.Win, 1, FieldSize + 1)));

        Assert.Equal("stake", refusal.ParamName);
        Assert.Empty(pool.Stakes);
    }

    [Fact]
    public void One_unknown_horse_among_the_named_ones_is_enough_to_refuse_the_ticket() {
        StakePool pool = OpenPool(BetType.Trifecta);

        Assert.Throws<ArgumentException>(() => pool.Accept(Tickets.Ticket(BetType.Trifecta, 1, 1, 2, FieldSize + 1)));
        Assert.Empty(pool.Stakes);
    }

    [Theory]
    [InlineData(BetType.Win, BetType.Place)]
    [InlineData(BetType.Place, BetType.Win)]
    [InlineData(BetType.Quinella, BetType.Exacta)]
    [InlineData(BetType.Exacta, BetType.Quinella)]
    public void A_stake_on_another_bet_type_cannot_enter_the_pool(BetType poolType, BetType ticketType) {
        StakePool pool = OpenPool(poolType);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => pool.Accept(AnyTicketOn(ticketType)));

        Assert.Equal("stake", refusal.ParamName);
        Assert.Empty(pool.Stakes);
    }

    [Fact]
    public void A_pool_cannot_accept_nothing() {
        StakePool pool = OpenPool();

        Assert.Throws<ArgumentNullException>(() => pool.Accept(null!));
    }

    [Fact]
    public void Closing_the_pool_at_the_off_stops_it_taking_stakes() {
        StakePool pool = OpenPool(BetType.Win);
        pool.Accept(AnyTicketOn(BetType.Win));

        pool.Close();

        Assert.Equal(PoolStatus.Closed, pool.Status);
        Assert.Throws<InvalidOperationException>(() => pool.Accept(AnyTicketOn(BetType.Win)));
        Assert.Single(pool.Stakes);
    }

    [Fact]
    public void A_pool_cannot_be_closed_twice() {
        StakePool pool = OpenPool();
        pool.Close();

        Assert.Throws<InvalidOperationException>(() => pool.Close());
        Assert.Equal(PoolStatus.Closed, pool.Status);
    }

    [Fact]
    public void A_settled_pool_accepts_no_stake_and_cannot_be_closed_again() {
        StakePool pool = OpenPool(BetType.Win);
        pool.Close();
        pool.Settle(Any.RaceResult().WithRace(pool.Race).Generate());

        Assert.Throws<InvalidOperationException>(() => pool.Accept(AnyTicketOn(BetType.Win)));
        Assert.Throws<InvalidOperationException>(() => pool.Close());
    }

    [Fact]
    public void An_open_pool_cannot_be_settled_yet() {
        StakePool pool = OpenPool();

        Assert.Throws<InvalidOperationException>(() => pool.Settle(Any.RaceResult().WithRace(pool.Race).Generate()));
        Assert.Equal(PoolStatus.Open, pool.Status);
    }

    [Fact]
    public void A_pool_is_settled_once() {
        StakePool pool = OpenPool();
        pool.Close();
        RaceResult anyResult = Any.RaceResult().WithRace(pool.Race).Generate();
        pool.Settle(anyResult);

        Assert.Throws<InvalidOperationException>(() => pool.Settle(anyResult));
        Assert.Equal(PoolStatus.Settled, pool.Status);
    }

    [Fact]
    public void Settling_a_closed_pool_marks_it_settled() {
        StakePool pool = OpenPool();
        pool.Close();

        pool.Settle(Any.RaceResult().WithRace(pool.Race).Generate());

        Assert.Equal(PoolStatus.Settled, pool.Status);
    }

    [Fact]
    public void The_result_of_another_race_cannot_settle_the_pool() {
        StakePool pool = OpenPool();
        pool.Close();
        Race       anotherRace = Any.Race().WithCode(code => code.Except(pool.Race.Code)).Generate();
        RaceResult otherResult = Any.RaceResult().WithRace(anotherRace).Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => pool.Settle(otherResult));

        Assert.Equal("result", refusal.ParamName);
        Assert.Equal(PoolStatus.Closed, pool.Status);
    }

    [Fact]
    public void A_pool_cannot_be_settled_by_nothing() {
        StakePool pool = OpenPool();
        pool.Close();

        Assert.Throws<ArgumentNullException>(() => pool.Settle(null!));
        Assert.Equal(PoolStatus.Closed, pool.Status);
    }

    [Fact]
    public void A_result_that_cannot_name_the_winning_combination_leaves_the_pool_closed() {
        Race      race = Any.Race().WithFieldSize(FieldSize).Generate();
        StakePool exacta = Tickets.ClosedPool(race, BetType.Exacta, 1.00m, Tickets.PlainRules());
        RaceResult onlyOneFinisher = Tickets.Result(race, new[] { 3 });

        Assert.Throws<InvalidOperationException>(() => exacta.Settle(onlyOneFinisher));
        Assert.Equal(PoolStatus.Closed, exacta.Status);
    }

}
