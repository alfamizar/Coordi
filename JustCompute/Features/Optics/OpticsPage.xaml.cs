using JustCompute.Shared.Controls;

namespace JustCompute.Features.Optics;

public partial class OpticsPage : BasePage
{
    public OpticsPage(OpticsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
