using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Controls;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using Utilities;

namespace GrfToWpfBridge {
	public static class Binder {
		private static readonly Dictionary<int, object> _binders = new Dictionary<int, object>();

		/// <summary>
		/// Adds a new binder format.
		/// </summary>
		/// <typeparam name="TElement">The type of the element.</typeparam>
		/// <typeparam name="TValue">The type of the value.</typeparam>
		/// <param name="binder">The binder.</param>
		internal static void AddBinder<TElement, TValue>(object binder) {
			_binders[typeof(TElement).GetHashCode() * typeof(TValue).GetHashCode()] = binder;
		}

		internal static BinderBase ChB = new CheckBoxBinder();
		internal static BinderBase MeI = new MenuItemBinder();
		internal static BinderBase TkI = new TkMenuItemBinder();
		internal static BinderBase CoB = new ComboBoxBinder();
		internal static BinderBase PbB = new PathBrowserBinder();
		internal static BinderBase TbString = new TextBoxBinder<string>(FormatConverters.StringConverter);
		internal static BinderBase TbInt = new TextBoxBinder<int>(FormatConverters.IntOrHexConverter);
		internal static BinderBase TbDouble = new TextBoxBinder<double>(FormatConverters.DoubleConverter);
		internal static BinderBase TbFloat = new TextBoxBinder<float>(FormatConverters.SingleConverter);
		internal static BinderBase PaB = new PasswordBoxBinder<string>(FormatConverters.StringConverter);
		internal static BinderBase FbB = new FancyButtonBinder();
		internal static BinderBase TbB = new ToggleButtonBinder();
		internal static BinderBase RbB = new RadioButtonBinder();

		/// <summary>
		/// Retrieves the ConfigAsker property name from the FrameworkElement object.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <returns>The ConfigAsker property name.</returns>
		public static string ElementToConfigProperty(FrameworkElement element) {
			return "[UI #" + element.Name.GetHashCode() + "]";
		}

		#region Generic binders
		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="get">The get method.</param>
		public static void Bind<TElement, TValue>(TElement element, Expression<Func<TValue>> get) where TElement : UIElement {
			Bind(element, get, null, false);
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		public static void Bind<TElement, TValue>(TElement element, Expression<Func<TValue>> get, Action extra) where TElement : UIElement {
			Bind(element, get, extra, false);
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		/// <param name="execute">Executes the action after this method.</param>
		public static void Bind<TElement, TValue>(TElement element, Expression<Func<TValue>> get, Action extra, bool execute) where TElement : UIElement {
			int hash = typeof(TElement).GetHashCode() * typeof(TValue).GetHashCode();

			if (!_binders.ContainsKey(hash)) {
				Type current = typeof(TElement).BaseType;

				while (current != null) {
					hash = current.GetHashCode() * typeof(TValue).GetHashCode();

					if (_binders.ContainsKey(hash)) {
						((BinderAbstract<TElement, TValue>)_binders[hash]).Bind(element, get, extra, execute);
						return;
					}

					current = current.BaseType;
				}
			}

			((BinderAbstract<TElement, TValue>)_binders[hash]).Bind(element, get, extra, execute);
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		public static void Bind<TElement, TValue>(TElement element, Func<TValue> get, Action<TValue> set) where TElement : UIElement {
			Bind(element, get, set, null, false);
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		public static void Bind<TElement, TValue>(TElement element, Func<TValue> get, Action<TValue> set, Action extra) where TElement : UIElement {
			Bind(element, get, set, extra, false);
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		/// <param name="execute">Executes the action after this method.</param>
		public static void Bind<TElement, TValue>(TElement element, Func<TValue> get, Action<TValue> set, Action extra, bool execute) where TElement : UIElement {
			int hash = typeof (TElement).GetHashCode() * typeof (TValue).GetHashCode();

			if (!_binders.ContainsKey(hash)) {
				Type current = typeof(TElement).BaseType;

				while (current != null) {
					hash = current.GetHashCode() * typeof (TValue).GetHashCode();

					if (_binders.ContainsKey(hash)) {
						((BinderAbstract<TElement, TValue>)_binders[hash]).Bind(element, get, set, extra, execute);
						return;
					}

					current = current.BaseType;
				}

				throw new Exception("No configuration binder has been found : type = " + typeof(TElement) + ", value = " + typeof(TValue));
			}

			((BinderAbstract<TElement, TValue>) _binders[hash]).Bind(element, get, set, extra, execute);
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		public static void Bind<TElement>(TElement element) where TElement : FrameworkElement {
			Bind(element, null, false, null);
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		public static void Bind<TElement>(TElement element, Action extra) where TElement : FrameworkElement {
			Bind(element, extra, false, null);
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="defValue">Default value.</param>
		public static void Bind<TElement>(TElement element, string defValue) where TElement : FrameworkElement {
			Bind(element, null, false, defValue);
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		/// <param name="execute">Executes the action after this method.</param>
		public static void Bind<TElement>(TElement element, Action extra, bool execute) where TElement : FrameworkElement {
			Bind(element, extra, execute, null);
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		/// <param name="execute">Executes the action after this method.</param>
		/// <param name="defaultValue">The default vlaue.</param>
		public static void Bind<TElement>(TElement element, Action extra, bool execute, string defaultValue) where TElement : FrameworkElement {
			string prop = "[UI #" + element.Name.GetHashCode() + "]";

			if (element is TextBox) {
				Bind(element, () => Configuration.ConfigAsker[prop, defaultValue ?? ""], v => Configuration.ConfigAsker[prop] = v, extra, execute);
			}
			else if (element is CheckBox) {
				Bind(element, () => FormatConverters.BooleanConverter(Configuration.ConfigAsker[prop, defaultValue ?? false.ToString()]), v => Configuration.ConfigAsker[prop] = v.ToString(), extra, execute);
			}
			else if (element is ComboBox) {
				Bind(element, () => FormatConverters.IntOrHexConverter(Configuration.ConfigAsker[prop, defaultValue ?? "0"]), v => Configuration.ConfigAsker[prop] = v.ToString(CultureInfo.InvariantCulture), extra, execute);
			}
			else if (element is FancyButton) {
				Bind(element, () => FormatConverters.BooleanConverter(Configuration.ConfigAsker[prop, defaultValue ?? false.ToString()]), v => Configuration.ConfigAsker[prop] = v.ToString(), extra, execute);
			}
			else {
				throw new Exception("Unsupported type : " + typeof (TElement));
			}
		}
		#endregion
	}
}