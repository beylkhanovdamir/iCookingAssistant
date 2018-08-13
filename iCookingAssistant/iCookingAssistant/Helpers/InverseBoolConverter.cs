using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace iCookingAssistant.Helpers
{
	public class InverseBoolConverter: IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool state;
			
			Boolean.TryParse(value?.ToString(), out state);

			return !state;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return false;
		}
	}
}
