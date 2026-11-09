using GuardMiss.Domain;
using JustDummies;
using Xunit;

namespace GuardMiss.Tests;

public sealed class SilentGuardTests {

    private static int Rejections<T>(IAny<T> any) {
        int rejected = 0;
        using (Any.UseSeed(12345)) {
            for (int i = 0; i < 200; i++) {
                try { any.Generate(); } catch (ArgumentException) { rejected++; }
            }
        }
        return rejected;
    }

    [Fact] public void A1_guard_below_property_assignment() => Assert.Equal(0, Rejections(new AnyTariff()));
    [Fact] public void A2_helper_below_property_assignment() => Assert.Equal(0, Rejections(new AnySlot()));
    [Fact] public void B1_regex_match_local() => Assert.Equal(0, Rejections(new AnyLotCode()));
    [Fact] public void B2_bool_local() => Assert.Equal(0, Rejections(new AnyLotPrefix()));
    [Fact] public void B3_parsed_local() => Assert.Equal(0, Rejections(new AnyWeek()));
    [Fact] public void B4_switch_bool_local() => Assert.Equal(0, Rejections(new AnySeat()));
    [Fact] public void D_field_copy() => Assert.Equal(0, Rejections(new AnyRole()));
}
