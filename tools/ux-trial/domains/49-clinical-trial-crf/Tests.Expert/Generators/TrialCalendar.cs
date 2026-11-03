// Hand-written. The domain adds days to its dates (study days, windows, screening period): a dummy date is drawn in a
// span where that arithmetic cannot leave DateOnly's range, as the dates guide asks for any date a code moves.

namespace ClinicalTrial.Domain;

public static class TrialCalendar {

    public static readonly DateOnly Earliest = new DateOnly(2000, 1, 1);
    public static readonly DateOnly Latest   = new DateOnly(2090, 12, 31);

}
