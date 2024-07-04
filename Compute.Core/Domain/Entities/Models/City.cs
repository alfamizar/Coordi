using CommunityToolkit.Mvvm.ComponentModel;

namespace Compute.Core.Domain.Entities.Models
{
    public partial class City : ObservableObject
    {
        [ObservableProperty]
        private string cityName = string.Empty;
        [ObservableProperty]
        private string countryName = string.Empty;
        [ObservableProperty]
        private int population;
    }
}
