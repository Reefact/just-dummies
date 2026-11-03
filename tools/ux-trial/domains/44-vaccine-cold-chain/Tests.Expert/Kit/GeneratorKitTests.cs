using ColdChain.Domain;
using ColdChain.Tests.Expert.Generators;
using ColdChain.Tests.Expert.Support;
using JustDummies;
using Xunit;

namespace ColdChain.Tests.Expert.Kit;

/// <summary>
///     The generators are test code the other tests lean on: a draw the domain would refuse must show up here,
///     by name, rather than as a mysterious failure inside a behaviour test.
/// </summary>
public sealed class GeneratorKitTests {

    private const int Draws = 300;

    [Fact]
    public void Every_value_object_generator_draws_values_the_domain_accepts() {
        for (int draw = 0; draw < Draws; draw++) {
            Any.ConsignmentId().Generate();
            Any.CustodianId().Generate();
            Any.StabilityBudget().Generate();
            Any.VaccineProduct().Generate();
            Any.DataLogger().Generate();
            Any.LoggerReading().Generate();
            Any.Excursion().Generate();
            Any.ReceivingWarehouse().Generate();
            Any.ReleaseDecision().Generate();
        }
    }

    [Fact]
    public void Every_entity_generator_draws_values_the_domain_accepts() {
        for (int draw = 0; draw < Draws; draw++) {
            Any.ColdChainPallet().Generate();
            Any.LegCustody().Generate();
            Any.TemperatureHistory().Generate();
            Any.ColdChainConsignment().Generate();
        }
    }

    [Theory]
    [InlineData(ReleaseOutcome.Released)]
    [InlineData(ReleaseOutcome.Quarantined)]
    [InlineData(ReleaseOutcome.Rejected)]
    public void A_release_decision_asked_for_an_outcome_draws_reasons_that_justify_it(ReleaseOutcome outcome) {
        for (int draw = 0; draw < Draws; draw++) {
            ReleaseDecision decision = Any.ReleaseDecision().WithOutcome(outcome).Generate();

            Assert.Equal(outcome, decision.Outcome);
            Assert.Equal(outcome == ReleaseOutcome.Quarantined, decision.QuarantineBay is not null);
        }
    }

    [Fact]
    public void A_logger_covering_days_is_calibrated_on_each_end_of_them() {
        DateOnly from = new DateOnly(2026, 3, 9);
        DateOnly to   = new DateOnly(2026, 3, 12);

        for (int draw = 0; draw < Draws; draw++) {
            DataLogger logger = Any.DataLogger().CoveringDays(from, to).Generate();

            Assert.True(logger.IsCalibratedOn(from));
            Assert.True(logger.IsCalibratedOn(to));
        }
    }

    [Fact]
    public void A_journey_can_be_dispatched_and_delivered_whatever_is_drawn_around_it() {
        for (int draw = 0; draw < 100; draw++) {
            ColdChainConsignment consignment = Journey.Planned().WithPalletCount(3).Generate();

            consignment.Dispatch(Any.CustodianId().Generate(), Journey.Start);
            consignment.Deliver(Journey.WarehouseFor(consignment).Generate(), Journey.Start.AddHours(1));

            Assert.Equal(ConsignmentStatus.Delivered, consignment.Status);
        }
    }
}
