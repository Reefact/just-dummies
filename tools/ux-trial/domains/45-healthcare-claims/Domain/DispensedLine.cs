using Clinical.Domain;
using Payments.Domain;

namespace HealthcareClaims.Domain;

/// <summary>
///     One line of a prescription as the pharmacy dispensed it: how many packs, at which unit price, and what the
///     statutory scheme already reimbursed on it. All amounts are in euros.
/// </summary>
public sealed class DispensedLine {

    public const int     MaxPacks     = 12;
    public const decimal MaxUnitPrice = 5_000m;

    public DispensedLine(PrescriptionLine      prescriptionLine,
                         ReimbursementCategory category,
                         int                   packs,
                         Money                 unitPrice,
                         Money?                statutoryReimbursement) {
        ArgumentNullException.ThrowIfNull(prescriptionLine);
        ArgumentNullException.ThrowIfNull(unitPrice);
        if (!Enum.IsDefined(category)) {
            throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown reimbursement category.");
        }
        if (packs < 1 || packs > MaxPacks) {
            throw new ArgumentOutOfRangeException(nameof(packs), packs, $"A pharmacy dispenses between 1 and {MaxPacks} packs of a line.");
        }
        if (unitPrice.Currency != Currency.EUR) {
            throw new ArgumentException("A French pharmacy prices in euros.", nameof(unitPrice));
        }
        ArgumentOutOfRangeException.ThrowIfGreaterThan(unitPrice.Amount, MaxUnitPrice, nameof(unitPrice));

        decimal pricePaid = unitPrice.Amount * packs;
        if (statutoryReimbursement is not null) {
            if (category == ReimbursementCategory.Unlisted) {
                throw new ArgumentException("An unlisted medicine is not reimbursed by the statutory scheme.", nameof(statutoryReimbursement));
            }
            if (statutoryReimbursement.Currency != Currency.EUR) {
                throw new ArgumentException("The statutory scheme reimburses in euros.", nameof(statutoryReimbursement));
            }
            if (statutoryReimbursement.Amount > pricePaid) {
                throw new ArgumentException($"The statutory scheme cannot reimburse {statutoryReimbursement} on a line that cost {pricePaid:0.00} EUR.", nameof(statutoryReimbursement));
            }
        }

        PrescriptionLine       = prescriptionLine;
        Category               = category;
        Packs                  = packs;
        UnitPrice              = unitPrice;
        StatutoryReimbursement = statutoryReimbursement;
        PricePaid              = pricePaid;
    }

    public PrescriptionLine      PrescriptionLine       { get; }
    public ReimbursementCategory Category               { get; }
    public int                   Packs                  { get; }
    public Money                 UnitPrice              { get; }
    public Money?                StatutoryReimbursement { get; }

    /// <summary>What the insured paid at the counter for this line: unit price times packs.</summary>
    public decimal PricePaid { get; }

    /// <summary>What remains at the insured's charge once the statutory scheme has paid; the ceiling of any supplementary reimbursement.</summary>
    public decimal RemainingCharge => PricePaid - (StatutoryReimbursement?.Amount ?? 0m);

}
