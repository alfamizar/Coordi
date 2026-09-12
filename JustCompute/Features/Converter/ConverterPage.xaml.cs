using JustCompute.Shared.Controls;

namespace JustCompute.Features.Converter;

public partial class ConverterPage : BasePage
{
    public ConverterPage(ConverterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
