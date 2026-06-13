using Color = System.Drawing.Color;

namespace Compute.Core.UI
{
    public interface IEnvironment
    {
        void SetNavigationBarColor(Color color);

        void ResetNavigationBarColor();
    }
}
