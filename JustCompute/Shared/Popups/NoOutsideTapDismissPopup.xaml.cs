using CommunityToolkit.Maui.Views;
using Compute.Core.UI;

namespace JustCompute.Shared.Popups;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class NoOutsideTapDismissPopup : Popup<DialogResult>
{
    public DialogButton SelectedButton { get; private set; } = DialogButton.None;

    public static readonly BindableProperty DialogTitleProperty = BindableProperty
        .Create(nameof(DialogTitle), typeof(string), typeof(NoOutsideTapDismissPopup));
    public string DialogTitle
    {
        get => (string)GetValue(DialogTitleProperty);
        set => SetValue(DialogTitleProperty, value);
    }

    public static readonly BindableProperty DialogMessageProperty = BindableProperty
        .Create(nameof(DialogMessage), typeof(string), typeof(NoOutsideTapDismissPopup));
    public string DialogMessage
    {
        get => (string)GetValue(DialogMessageProperty);
        set => SetValue(DialogMessageProperty, value);
    }

    public static readonly BindableProperty PositiveButtonTextProperty = BindableProperty
        .Create(nameof(PositiveButtonText), typeof(string), typeof(NoOutsideTapDismissPopup));
    public string PositiveButtonText
    {
        get => (string)GetValue(PositiveButtonTextProperty);
        set => SetValue(PositiveButtonTextProperty, value);
    }

    public static readonly BindableProperty NegativeButtonTextProperty = BindableProperty
        .Create(nameof(NegativeButtonText), typeof(string), typeof(NoOutsideTapDismissPopup));
    public string? NegativeButtonText
    {
        get => (string?)GetValue(NegativeButtonTextProperty);
        set => SetValue(NegativeButtonTextProperty, value);
    }

    public static readonly BindableProperty NeutralButtonTextProperty = BindableProperty
        .Create(nameof(NeutralButtonText), typeof(string), typeof(NoOutsideTapDismissPopup));
    public string? NeutralButtonText
    {
        get => (string?)GetValue(NeutralButtonTextProperty);
        set => SetValue(NeutralButtonTextProperty, value);
    }

    public NoOutsideTapDismissPopup()
    {
        InitializeComponent();
    }

    async void PositiveButtonClicked(object sender, EventArgs e)
    {
        SelectedButton = DialogButton.Positive;
        await CloseAsync(new DialogResult(SelectedButton));
    }

    async void NegativeButtonClicked(object sender, EventArgs e)
    {
        SelectedButton = DialogButton.Negative;
        await CloseAsync(new DialogResult(SelectedButton));
    }

    async void NeutralButtonClicked(object sender, EventArgs e)
    {
        SelectedButton = DialogButton.Neutral;
        await CloseAsync(new DialogResult(SelectedButton));
    }
}
