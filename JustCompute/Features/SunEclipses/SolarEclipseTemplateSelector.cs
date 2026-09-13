using Compute.Core.Domain.Entities.Models.Eclipses;

namespace JustCompute.Features.SunEclipses
{
    public class SolarEclipseTemplateSelector : DataTemplateSelector
    {
        public DataTemplate VisibleTemplate { get; set; } = null!;
        public DataTemplate NotVisibleTemplate { get; set; } = null!;

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            return item is SolarEclipseInfo { IsVisible: true }
                ? VisibleTemplate
                : NotVisibleTemplate;
        }
    }
}
