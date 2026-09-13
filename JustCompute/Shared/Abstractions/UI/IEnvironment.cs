using Color = System.Drawing.Color;

namespace JustCompute.Shared.Abstractions.UI
{
    public interface IEnvironment
    {
        void SetNavigationBarColor(Color color);

        void ResetNavigationBarColor();
    }
}
