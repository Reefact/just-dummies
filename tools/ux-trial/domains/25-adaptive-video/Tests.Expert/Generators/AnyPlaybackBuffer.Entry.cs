// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PlaybackBuffer --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>Hangs <c>Any.PlaybackBuffer()</c> off the library's own entry point.</summary>
public static class AnyPlaybackBufferEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>PlaybackBuffer</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyPlaybackBuffer PlaybackBuffer() {
            return new AnyPlaybackBuffer();
        }

    }

}
