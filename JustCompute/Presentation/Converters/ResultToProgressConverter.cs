using Compute.Core.Extensions;
using System.Globalization;

namespace JustCompute.Presentation.Converters
{
    public class ResultToProgressConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string? totalVal = values[0] as string;
            string? subResultlVal = values[1] as string;

            if (totalVal?.IsSet() == true && subResultlVal?.IsSet() == true)
            {
                return CalculateProgress(float.Parse(totalVal), float.Parse(subResultlVal));
            }
            else
            {
                return 0;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static float CalculateProgress(float total, float subResult)
        {
            if (total > 0)
            {
                float result = (subResult / total);
                return result;
            }
            else return 0;
        }
    }
}
