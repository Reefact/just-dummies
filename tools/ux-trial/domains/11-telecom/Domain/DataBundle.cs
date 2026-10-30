namespace Telecom.Domain;

/// <summary>An add-on data bundle bought on top of a plan: sold in steps of 100 MB, priced in cents.</summary>
public sealed class DataBundle {

    public DataBundle(int megabytes, decimal price, TimeSpan validity) {
        if (megabytes < 100 || megabytes > 100_000) {
            throw new ArgumentOutOfRangeException(nameof(megabytes), megabytes, "A bundle holds 100 MB to 100 GB.");
        }
        if (megabytes % 100 != 0) {
            throw new ArgumentException("Bundles are sold in steps of 100 MB.", nameof(megabytes));
        }
        ArgumentOutOfRangeException.ThrowIfLessThan(price, 0.01m);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(price, 500m);
        ArgumentOutOfRangeException.ThrowIfLessThan(validity, TimeSpan.FromDays(1));

        Megabytes = megabytes;
        Price     = price;
        Validity  = validity;
    }

    public int      Megabytes { get; }
    public decimal  Price     { get; }
    public TimeSpan Validity  { get; }

    public decimal PricePerGigabyte => decimal.Round(Price * 1024m / Megabytes, 2);

}
