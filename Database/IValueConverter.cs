using System;
using System.Collections.Generic;
using System.ComponentModel;
using Utilities;

namespace Database {
	public interface IValueConverter {
		object ConvertTo(Tuple source, object value);
		T ConvertFrom<T>(Tuple source, object value);
	}

	public static class ValueConverter {
		public static DefaultValueConverter DefaultConverter = new DefaultValueConverter();
	}

	public static class DataCopyParser {
		public static DefaultDataCopyParser DefaultDataCopyParser = new DefaultDataCopyParser();
	}

	public class DefaultDataCopyParser : IDataCopy {
		public object CopyFrom(object value) {
			return value;
		}
	}

	public class TableCopyParser<TKey, TValue> : IDataCopy {
		public object CopyFrom(object value) {
			if (value is Dictionary<TKey, TValue>) {
				var table = (Dictionary<TKey, TValue>) value;
				Dictionary<TKey, TValue> newTable = new Dictionary<TKey, TValue>(table.Count);
				foreach (var item in table) {
					newTable[item.Key] = item.Value;
				}
				return newTable;
			}
			return value;
		}
	}

	public interface IDataCopy {
		object CopyFrom(object value);
	}

	public class IntValueConverter : IValueConverter {
		public object ConvertTo(Tuple source, object value) {
			throw new NotImplementedException();
		}

		public T ConvertFrom<T>(Tuple source, object value) {
			var type = value.GetType();

			if (type == typeof(string)) {
				var valueS = (string)value;

				if (valueS == "")
					return (T)(object)0;

				return (T)(object)FormatConverters.IntOrHexConverter(valueS);
			}

			if (type == typeof(Boolean)) {
				var valueS = value.ToString();
				return (T)(object)(FormatConverters.BooleanConverter(valueS == "" ? "false" : valueS) ? 1 : 0);
			}

			if (TypeDescriptor.GetConverter(value).CanConvertTo(typeof(T)))
				return (T)TypeDescriptor.GetConverter(value).ConvertTo(value, typeof(T));

			return (T)value;
		}
	}

	public class StringValueConverter : IValueConverter {
		public object ConvertTo(Tuple source, object value) {
			throw new NotImplementedException();
		}

		public T ConvertFrom<T>(Tuple source, object value) {
			return (T)(object)value.ToString();
		}
	}

	public class BooleanValueConverter : IValueConverter {
		public object ConvertTo(Tuple source, object value) {
			throw new NotImplementedException();
		}

		public T ConvertFrom<T>(Tuple source, object value) {
			var type = value.GetType();

			if (type == typeof(string)) {
				var valueS = (string)value;
				return (T)(object)FormatConverters.BooleanConverter(valueS == "" ? "false" : valueS);
			}

			if (type == typeof(int)) {
				var valueI = (int)value;

				if (valueI != 0)
					return (T)(object)true;
				return (T)(object)false;
			}

			if (TypeDescriptor.GetConverter(value).CanConvertTo(typeof(T)))
				return (T)TypeDescriptor.GetConverter(value).ConvertTo(value, typeof(T));

			return (T)value;
		}
	}

	public static class DefaultValueConverters {
		public static IntValueConverter IntValueConverter = new IntValueConverter();
		public static StringValueConverter StringValueConverter = new StringValueConverter();
		public static BooleanValueConverter BooleanValueConverter = new BooleanValueConverter();
	}

	public class DefaultValueConverter : IValueConverter {
		public object ConvertTo(Tuple source, object value) {
			return value;
		}

		public T ConvertFrom<T>(Tuple source, object value) {
			if (value == null)
				return default(T);

			if (value.GetType() == typeof(T))
				return (T)value;

			// Apply some basic conversions
			if (typeof(T) == typeof(int)) {
				return DefaultValueConverters.IntValueConverter.ConvertFrom<T>(source, value);
			}

			if (typeof(T) == typeof(string)) {
				return DefaultValueConverters.StringValueConverter.ConvertFrom<T>(source, value);
			}

			if (typeof(T) == typeof(bool)) {
				return DefaultValueConverters.BooleanValueConverter.ConvertFrom<T>(source, value);
			}

			if (TypeDescriptor.GetConverter(value).CanConvertTo(typeof(T)))
				return (T)TypeDescriptor.GetConverter(value).ConvertTo(value, typeof(T));

			return (T)value;
		}
	}
}
