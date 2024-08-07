﻿using Color = System.Drawing.Color;

namespace Compute.Core.UI
{
    public interface IEnvironment
    {
        void SetStatusBarColor(Color color, bool statusBarTint);

        void SetNavigationBarColor(Color color);

        void ResetNavigationBarColor();
    }
}