using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace TokeiLibrary.WPF.Styles.ListView {
	public abstract class ConverterGridViewColumn : GridViewColumn, IValueConverter {
		private readonly Type bindingType;

		protected ConverterGridViewColumn(Type bindingType) {
			if (bindingType == null) {
				throw new ArgumentNullException("bindingType");
			}

			this.bindingType = bindingType;


			Binding binding = new Binding();
			binding.Mode = BindingMode.OneWay;
			binding.Converter = this;
			DisplayMemberBinding = binding;
		}


		public Type BindingType {
			get { return bindingType; }
		}

		#region IValueConverter Members

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if (!bindingType.IsInstanceOfType(value)) {
				throw new InvalidOperationException();
			}
			return ConvertValue(value);
		}


		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}

		#endregion

		protected abstract object ConvertValue(object value);
	}
}