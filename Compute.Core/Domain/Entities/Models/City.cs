using CommunityToolkit.Mvvm.ComponentModel;

namespace Compute.Core.Domain.Entities.Models
{
    public partial class City : ObservableObject
    {
        /// <summary>
        /// Row id of this city in the user's database. Needed to update or delete the right row:
        /// the location's own id is a different sequence and only ever matched by coincidence.
        /// </summary>
        public int Id { get; set; }

        [ObservableProperty]
        private string cityName = string.Empty;
        [ObservableProperty]
        private string countryName = string.Empty;
        [ObservableProperty]
        private int population;
    }
}
