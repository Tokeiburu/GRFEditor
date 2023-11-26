using System;
using System.Reflection;

namespace Utilities {
	public class Setting {
		private readonly Action<object> _set;
		private readonly Func<object> _get;
		private readonly object _obj;
		private readonly PropertyInfo _property;

		public Setting(object obj, PropertyInfo property) {
			_obj = obj;
			_property = property;
		}

		public Setting(Action<object> set, Func<object> get) {
			_set = set;
			_get = get;
		}

		public void Set(object value) {
			if (_set != null) {
				_set(value);
			}
			else {
				_property.SetValue(_obj, value, null);
			}
		}

		public object Get() {
			return _get != null ? _get() : _property.GetValue(_obj, null);
		}

		public static Setting Make<T>(string propertyName) {
			return new Setting(null, typeof(T).GetProperty(propertyName));
		}
	}

	public class TypeSetting<T> {
		private readonly Action<T> _set;
		private readonly Func<T> _get;
		private readonly object _obj;
		private readonly PropertyInfo _property;

		public TypeSetting(object obj, PropertyInfo property) {
			_obj = obj;
			_property = property;
		}

		public TypeSetting(Action<T> set, Func<T> get) {
			_set = set;
			_get = get;
		}

		public void Set(T value) {
			if (_set != null) {
				_set(value);
			}
			else {
				_property.SetValue(_obj, value, null);
			}
		}

		public T Get() {
			return (T) (_get != null ? _get() : _property.GetValue(_obj, null));
		}
	}
}
