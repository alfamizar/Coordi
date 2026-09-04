namespace JustCompute.Shared.Theming
{
    /// <summary>
    /// The themes offered in Settings. Persisted by name, so the order may change freely but the
    /// names may not.
    /// </summary>
    public enum AppThemeId
    {
        /// <summary>Follows the device: <see cref="Ocean"/> by day, <see cref="Midnight"/> by night.</summary>
        System,

        /// <summary>The original palette: bright cyan-blue over pale blue cards.</summary>
        Ocean,

        /// <summary>Rose pink chrome over mint-green cards.</summary>
        Blossom,

        /// <summary>The original dark palette: crimson and amber on black.</summary>
        Midnight,

        /// <summary>Orange on near-black, after Penombre's dark orange scheme.</summary>
        Ember,
    }
}
