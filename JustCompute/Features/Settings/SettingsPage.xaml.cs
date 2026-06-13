
using JustCompute.Shared.Controls;
using JustCompute.Shared.ViewModels;

namespace JustCompute.Features.Settings
{
    public partial class SettingsPage : BasePage
    {
        public SettingsPage(SettingsViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}