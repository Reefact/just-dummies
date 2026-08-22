namespace JustDummies;

/// <summary>How the path component is drawn.</summary>
internal enum UriPathMode {

    Auto, // 0 to 2 arbitrary segments
    Root, // no segments (an authority family still renders "/")
    Exact // a fixed number of segments

}
