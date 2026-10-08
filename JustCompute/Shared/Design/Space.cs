namespace JustCompute.Shared.Design
{
    /// <summary>
    /// A step of the spacing scale in Styles.xaml — Micro 4, Small 8, Medium 12, Large 16,
    /// XLarge 24 — by name, so that a screen asks for a step instead of restating its number.
    /// <see cref="Unset"/> is what every side of an <see cref="InsetExtension"/> starts as: it is
    /// how a side nobody named is told apart from one named <see cref="Zero"/>.
    /// </summary>
    public enum Space
    {
        Unset,
        Zero,
        Micro,
        Small,
        Medium,
        Large,
        XLarge,
    }
}
