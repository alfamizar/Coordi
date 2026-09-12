using JustCompute.Shared.Controls;

namespace JustCompute.Features.SkyChart;

public partial class SkyChartPage : BasePage
{
    /// <summary>
    /// How much of the page's height the disc may take. The rest has to hold the instant being
    /// shown and the controls that change it, and a chart with its scrubber off-screen is a
    /// picture rather than an instrument.
    ///
    /// Six tenths left the second toggle clipped on a 7-inch tablet in landscape, which is the
    /// shortest viewport the app is built for: 600 points of height against roughly 190 the card
    /// needs. Phones are unaffected — there the disc is bound by width long before this.
    /// </summary>
    private const double HeightShare = 0.55;

    public SkyChartPage(SkyChartViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// The disc is square where there is room for it and height-bound where there is not: bound
    /// to its own width it grew to the width of a landscape tablet, which is taller than the
    /// screen. The drawable centres the disc in whatever rectangle it is given, so a wide, short
    /// one leaves margins rather than distorting anything.
    /// </summary>
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width <= 0 || height <= 0) return;

        Chart.HeightRequest = Math.Min(width, height * HeightShare);
    }
}
