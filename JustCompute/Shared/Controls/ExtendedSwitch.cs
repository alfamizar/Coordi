namespace JustCompute.Shared.Controls
{
    public class ExtendedSwitch : Switch
    {
        public static new readonly BindableProperty OffColorProperty = BindableProperty.Create(nameof(OffColor),
                                                                        typeof(Color), typeof(ExtendedSwitch));
        public new Color OffColor
        {
            get { return (Color)GetValue(OffColorProperty); }
            set { SetValue(OffColorProperty, value); }
        }
    }
}
