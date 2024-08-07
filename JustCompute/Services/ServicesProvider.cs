﻿namespace JustCompute.Services
{
    public static class ServicesProvider
	{
		public static TService GetService<TService>()
			=> Current.GetService<TService>();

		private static IServiceProvider Current
			=>
#if WINDOWS10_0_17763_0_OR_GREATER
			MauiWinUIApplication.Current.Services;
#elif ANDROID
			IPlatformApplication.Current.Services;
#elif IOS || MACCATALYST
            IPlatformApplication.Current.Services;
#else
			null;
#endif
    }
}
