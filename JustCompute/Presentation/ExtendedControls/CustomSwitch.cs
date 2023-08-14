namespace JustCompute.Presentation.ExtendedControls
{
    public class CustomSwitch : Switch
    {
        public static readonly BindableProperty OffColorProperty = BindableProperty
            .Create(nameof(OffColor), typeof(Color), typeof(CustomSwitch), default);

        public static readonly BindableProperty TrackOnColorProperty = BindableProperty
            .Create(nameof(TrackOnColor), typeof(Color), typeof(CustomSwitch), default);

        public static readonly BindableProperty TrackOffColorProperty = BindableProperty
            .Create(nameof(TrackOffColor), typeof(Color), typeof(CustomSwitch), default);

        public static readonly BindableProperty ThumbOnIconProperty = BindableProperty
            .Create(nameof(ThumbOnIcon), typeof(string), typeof(CustomSwitch), null);

        public static readonly BindableProperty ThumbOffIconProperty = BindableProperty
            .Create(nameof(ThumbOffIcon), typeof(string), typeof(CustomSwitch), null);

        public Color OffColor
        {
            get => (Color)GetValue(OffColorProperty);
            set => SetValue(OffColorProperty, value);
        }

        public Color TrackOnColor
        {
            get => (Color)GetValue(TrackOnColorProperty);
            set => SetValue(TrackOnColorProperty, value);
        }
        public Color TrackOffColor
        {
            get => (Color)GetValue(TrackOffColorProperty);
            set => SetValue(TrackOffColorProperty, value);
        }

        public string ThumbOnIcon
        {
            get => (string)GetValue(ThumbOnIconProperty);
            set => SetValue(ThumbOnIconProperty, value);
        }

        public string ThumbOffIcon
        {
            get => (string)GetValue(ThumbOffIconProperty);
            set => SetValue(ThumbOffIconProperty, value);
        }
    }
}
