namespace Compute.Core.Domain.Entities.Models.Moon
{
    public enum MoonPhase
    {
        //
        // Summary:
        //     New Moon (1.0/0.0 phase) reached on specified day.
        NewMoon,
        //
        // Summary:
        //     Waxing Crescent. Phase between 0.0 and 0.25 on specified day.
        WaxingCrescent,
        //
        // Summary:
        //     First Quarter (0.25 phase) reached on specified day.
        FirstQuarter,
        //
        // Summary:
        //     Waxing Gibbous. Phase between 0.25 and 0.5 on specified day.
        WaxingGibbous,
        //
        // Summary:
        //     Full Moon (0.5 phase) reached on specified day
        FullMoon,
        //
        // Summary:
        //     Waning Gibbous. Phase between 0.5 and 0.75 on specified day.
        WaningGibbous,
        //
        // Summary:
        //     Last Quarter (0.75 phase) reached on specified day.
        LastQuarter,
        //
        // Summary:
        //     Waning Crescent. Phase between 0.75 and 1.0 on specified day.
        WaningCrescent
    }
}
