using System;
using System.Globalization;
using UIKit;

namespace Xamarin.Forms.Platform.iOS
{
	public class UIColorConverter : INativeValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Color)
				return ((Color)value).ToUIColor();

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is UIColor)
				return ((UIColor)value).ToColor();

			return null;
		}
	}
}

