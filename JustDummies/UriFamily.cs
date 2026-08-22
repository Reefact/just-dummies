namespace JustDummies;

/// <summary>The URI families the builder can produce, one per distinct valid shape.</summary>
internal enum UriFamily {

    Web,       // http / https  — full authority: host, userinfo, port, path, query, fragment
    WebSocket, // ws   / wss    — authority without userinfo or fragment (RFC 6455)
    Ftp,       // ftp           — authority without query or fragment
    Mailto,    // mailto        — local@domain (+ optional headers)
    Relative   // no scheme     — path (+ optional query, fragment)

}
