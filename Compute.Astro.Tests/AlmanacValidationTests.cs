namespace Compute.Astro.Tests;

/// <summary>
/// Reference checks for the calendar/folklore layer: the Moon's zodiac sign and the
/// traditional full-moon names. Full-moon instants come from the validated
/// <see cref="MoonPhase"/> routines; dates are anchored to known real full moons (UTC).
/// </summary>
public class AlmanacValidationTests
{
    [Fact]
    public void MoonZodiacSign_MeeusExample47a()
    {
        // 1992-04-12 (JDE 2448724.5): Moon apparent longitude ≈ 133.16° → Leo.
        Assert.Equal(ZodiacSign.Leo, Zodiac.OfMoon(2448724.5));
    }

    [Fact]
    public void FullMoonNames_2024Monthlies()
    {
        Assert.Equal(FullMoonName.Wolf, Almanac.MoonNameOn(2024, 1, 25));       // 2024-01-25 17:54 UTC
        Assert.Equal(FullMoonName.Strawberry, Almanac.MoonNameOn(2024, 6, 22)); // 2024-06-22 01:08 UTC
        Assert.Equal(FullMoonName.Corn, Almanac.MoonNameOn(2024, 9, 18));       // 2024-09-18 02:34 UTC
        Assert.Equal(FullMoonName.Cold, Almanac.MoonNameOn(2024, 12, 15));      // 2024-12-15 09:02 UTC
    }

    [Fact]
    public void NoFullMoon_ReturnsNone()
    {
        // A date with no full moon in that month-cycle.
        Assert.Equal(FullMoonName.None, Almanac.MoonNameOn(2024, 1, 10));
    }

    [Fact]
    public void BlueMoon_August2023()
    {
        // August 2023 had two full moons: Aug 1 (Sturgeon) and Aug 31 (Blue).
        var august = Almanac.FullMoonsInMonth(2023, 8);
        Assert.Equal(2, august.Count);
        Assert.Equal(FullMoonName.Sturgeon, august[0].Name);
        Assert.Equal(1, august[0].Day);
        Assert.Equal(FullMoonName.Blue, august[1].Name);
        Assert.Equal(31, august[1].Day);
    }

    [Fact]
    public void TraditionalNameTable()
    {
        Assert.Equal(FullMoonName.Worm, Almanac.TraditionalName(3));
        Assert.Equal(FullMoonName.Hunters, Almanac.TraditionalName(10));
        Assert.Equal(FullMoonName.None, Almanac.TraditionalName(13));
    }
}
