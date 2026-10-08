namespace JustCompute.Shared.Design
{
    /// <summary>
    /// <c>{design:Inset Large}</c>, <c>{design:Inset Horizontal=Large, Vertical=Small}</c>,
    /// <c>{design:Inset Top=Micro}</c>: a Padding or Margin built from steps of the spacing scale,
    /// for the insets no style gives a screen. A side named on its own wins over its axis, and an
    /// axis over <see cref="All"/>; a side nothing names is zero.
    ///
    /// The steps come from the app's resources, so a change to the scale reaches every screen.
    /// Styles.xaml cannot use this, being where those values are read from, and states its own
    /// insets as numbers.
    /// </summary>
    [ContentProperty(nameof(All))]
    [AcceptEmptyServiceProvider]
    public class InsetExtension : IMarkupExtension<Thickness>
    {
        public Space All { get; set; }

        public Space Horizontal { get; set; }

        public Space Vertical { get; set; }

        public Space Left { get; set; }

        public Space Top { get; set; }

        public Space Right { get; set; }

        public Space Bottom { get; set; }

        public Thickness ProvideValue() => new(
            DesignTokens.Spacing(Pick(Left, Horizontal)),
            DesignTokens.Spacing(Pick(Top, Vertical)),
            DesignTokens.Spacing(Pick(Right, Horizontal)),
            DesignTokens.Spacing(Pick(Bottom, Vertical)));

        Thickness IMarkupExtension<Thickness>.ProvideValue(IServiceProvider serviceProvider) => ProvideValue();

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue();

        private Space Pick(Space side, Space axis) =>
            side != Space.Unset ? side : axis != Space.Unset ? axis : All;
    }
}
