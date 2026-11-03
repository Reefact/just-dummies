namespace MusicScore.Domain;

public sealed partial class AnyVoiceEntry {

    /// <summary>A rest: no pitch, and therefore nothing to tie.</summary>
    public AnyVoiceEntry Rest() {
        return WithoutPitch().WithTiedToNext(false);
    }

}
