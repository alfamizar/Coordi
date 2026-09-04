namespace Compute.Core.Domain.Entities.Models.Eclipses
{
    /// <summary>
    /// What the eclipse screens need from an eclipse, whatever kind it is: when it happens, and
    /// whether it can be seen from where the table was computed for.
    ///
    /// Solar and lunar eclipses have almost nothing else in common — one is local and the other
    /// global — but the screen that lists them groups by year and filters by visibility, and that
    /// is the whole of it.
    /// </summary>
    public interface IEclipseInfo
    {
        DateTime Date { get; }

        bool IsVisible { get; }
    }
}
