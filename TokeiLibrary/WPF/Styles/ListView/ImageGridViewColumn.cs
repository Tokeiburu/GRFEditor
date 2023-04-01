using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace TokeiLibrary.WPF.Styles.ListView {
	public abstract class ImageGridViewColumn : GridViewColumn, IValueConverter {
		protected ImageGridViewColumn() :
			this(Stretch.None) {
		}


		protected ImageGridViewColumn(Stretch imageStretch) {
			FrameworkElementFactory imageElement = new FrameworkElementFactory(typeof (Image));


			Binding imageSourceBinding = new Binding();
			imageSourceBinding.Converter = this;
			imageSourceBinding.Mode = BindingMode.OneWay;
			imageElement.SetBinding(Image.SourceProperty, imageSourceBinding);


			Binding imageStretchBinding = new Binding();
			imageStretchBinding.Source = imageStretch;
			imageElement.SetBinding(Image.StretchProperty, imageStretchBinding);

			DataTemplate template = new DataTemplate();
			template.VisualTree = imageElement;
			CellTemplate = template;
		}

		#region IValueConverter Members

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return GetImageSource(value);
		}


		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}

		#endregion

		protected abstract ImageSource GetImageSource(object value);
	}
}