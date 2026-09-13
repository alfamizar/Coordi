using JustCompute.Shared.Controls;

namespace JustCompute.Features.Planets;

public partial class PlanetsPage : BasePage
{
    public PlanetsPage(PlanetsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
