using JustCompute.Handlers.ExtendedSearchBar;

namespace JustCompute.DependencyInjectionExtensions
{
    public static class MauiHandlersExtensions
    {
        public static MauiAppBuilder ConfigureMauiHandlers(this MauiAppBuilder builder)
        {
            builder.ConfigureMauiHandlers(collection =>
            {
                collection.AddHandler<SearchBar, SearchBarExHandler>();
            });

            AllowNegativeNumbers();
            return builder;
        }

        /// <summary>
        /// Lets a numeric field accept a minus sign.
        ///
        /// <c>Keyboard="Numeric"</c> maps to a keypad with no negative key: digits and a decimal
        /// separator only. That is fine for a focal length, and wrong for anything that can go
        /// below zero — a latitude in the southern hemisphere, a longitude in the western one, a
        /// declination. Those fields were simply untypable on a phone.
        /// </summary>
        private static void AllowNegativeNumbers()
        {
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(
                "SignedNumericKeyboard",
                (handler, view) =>
                {
                    if (view.Keyboard != Keyboard.Numeric)
                    {
                        return;
                    }
#if ANDROID
                    handler.PlatformView.InputType =
                        Android.Text.InputTypes.ClassNumber
                        | Android.Text.InputTypes.NumberFlagDecimal
                        | Android.Text.InputTypes.NumberFlagSigned;
#elif IOS
                    handler.PlatformView.KeyboardType = UIKit.UIKeyboardType.NumbersAndPunctuation;
#endif
                });
        }
    }
}