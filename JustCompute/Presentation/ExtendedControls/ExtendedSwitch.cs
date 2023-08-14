namespace JustCompute.Presentation.ExtendedControls
{
    public class ExtendedSwitch : Switch
    {
        public static readonly BindableProperty OffColorProperty = BindableProperty.Create(nameof(OffColor),
                                                                        typeof(Color), typeof(ExtendedSwitch));
        public Color OffColor
        {
            get { return (Color)GetValue(OffColorProperty); }
            set { SetValue(OffColorProperty, value); }
        }
    }
}
