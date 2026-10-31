using System.Text.RegularExpressions;

namespace AdaptiveVideo.Domain;

/// <summary>One rendition of the content in the bitrate ladder (e.g. "720p30").</summary>
public sealed class EncodingVariant
{
    private static readonly Regex IdFormat = new("^[0-9]{3,4}p(24|25|30|50|60)$", RegexOptions.CultureInvariant);

    public EncodingVariant(string id, int width, int height, Bitrate bitrate)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(bitrate);
        if (!IdFormat.IsMatch(id))
        {
            throw new FormatException($"Variant id '{id}' must look like '<height>p<fps>', for example '720p30'.");
        }

        if (width < 128 || width > 7680)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be between 128 and 7680 pixels.");
        }

        if (height < 96 || height > 4320)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be between 96 and 4320 pixels.");
        }

        if (width % 2 != 0 || height % 2 != 0)
        {
            throw new ArgumentException("Width and height must be even (4:2:0 chroma subsampling).");
        }

        if (height > width)
        {
            throw new ArgumentException($"Height {height} cannot exceed width {width}: only landscape content is supported.");
        }

        int declaredHeight = int.Parse(id[..id.IndexOf('p')], System.Globalization.CultureInfo.InvariantCulture);
        if (declaredHeight != height)
        {
            throw new ArgumentException($"Variant id '{id}' declares height {declaredHeight} but the height is {height}.");
        }

        Id = id;
        Width = width;
        Height = height;
        Bitrate = bitrate;
        FramesPerSecond = int.Parse(id[(id.IndexOf('p') + 1)..], System.Globalization.CultureInfo.InvariantCulture);
    }

    public string Id { get; }

    public int Width { get; }

    public int Height { get; }

    public int FramesPerSecond { get; }

    public Bitrate Bitrate { get; }

    public override string ToString() => $"{Id} @ {Bitrate}";
}
