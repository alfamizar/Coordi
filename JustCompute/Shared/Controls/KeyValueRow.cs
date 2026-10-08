using JustCompute.Shared.Design;

namespace JustCompute.Shared.Controls
{
    /// <summary>
    /// One row of a card's table: what it is on the left, its value on the right.
    ///
    /// Every card that lists measurements builds its rows from this, so the label column, the
    /// value's alignment and the gap between them are the same on every screen; before, some cards
    /// wrote "Sunrise: 7:11" as a sentence and others set the two apart, and the values were hard
    /// to find in the sentences. A row with nothing to show is hidden whole through IsVisible, and
    /// rows stacked in a VerticalStackLayout leave no gap where it was — a Grid with fixed rows
    /// keeps its spacing around an empty one.
    /// </summary>
    public class KeyValueRow : ContentView
    {
        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title), typeof(string), typeof(KeyValueRow), string.Empty,
            propertyChanged: (b, _, n) => ((KeyValueRow)b)._title.Text = (string?)n);

        public static readonly BindableProperty ValueProperty = BindableProperty.Create(
            nameof(Value), typeof(string), typeof(KeyValueRow), string.Empty,
            propertyChanged: (b, _, n) => ((KeyValueRow)b)._value.Text = (string?)n);

        public static readonly BindableProperty IsValueEmphasizedProperty = BindableProperty.Create(
            nameof(IsValueEmphasized), typeof(bool), typeof(KeyValueRow), false,
            propertyChanged: (b, _, n) => ((KeyValueRow)b)._value.FontAttributes = (bool)n ? FontAttributes.Bold : FontAttributes.None);

        private readonly Label _title = new() { VerticalOptions = LayoutOptions.Center };

        private readonly Label _value = new()
        {
            HorizontalTextAlignment = TextAlignment.End,
            VerticalOptions = LayoutOptions.Center,
        };

        public KeyValueRow()
        {
            // The value takes the width it needs and the label wraps in what is left: values here
            // are short — times, angles, distances — while a label can run to a line and a half.
            var row = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                ColumnSpacing = DesignTokens.Spacing(Space.Medium),
            };
            row.Add(_title, 0, 0);
            row.Add(_value, 1, 0);
            Content = row;
        }

        /// <summary>What the value is: "Sunrise", "Magnitude".</summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>The value, already formatted.</summary>
        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>Bold, for the one figure a card is mostly there to give.</summary>
        public bool IsValueEmphasized
        {
            get => (bool)GetValue(IsValueEmphasizedProperty);
            set => SetValue(IsValueEmphasizedProperty, value);
        }
    }
}
