using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TokeiLibrary.WPF.Styles;

namespace GrfToWpfBridge {
	public abstract class BinderBase {
	}

	public class CheckBoxBinder : BinderAbstract<CheckBox, bool> {
		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		/// <param name="execute">Executes the action after this method.</param>
		public override void Bind(CheckBox element, Func<bool> get, Action<bool> set, Action extra, bool execute) {
			element.IsChecked = get();

			element.Checked += (e, a) => {
				set(true);

				if (extra != null)
					extra();
			};

			element.Unchecked += (e, a) => {
				set(false);

				if (extra != null)
					extra();
			};

			if (execute) {
				if (extra != null)
					extra();
			}
		}
	}

	public class FancyButtonBinder : BinderAbstract<FancyButton, bool> {
		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		/// <param name="execute">Executes the action after this method.</param>
		public override void Bind(FancyButton element, Func<bool> get, Action<bool> set, Action extra, bool execute) {
			element.IsPressed = get();

			element.Click += (e, a) => {
				set(!get());

				if (extra != null)
					extra();
			};

			if (execute) {
				if (extra != null)
					extra();
			}
		}
	}

	public class ToggleButtonBinder : BinderAbstract<ToggleButton, bool> {
		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		/// <param name="execute">Executes the action after this method.</param>
		public override void Bind(ToggleButton element, Func<bool> get, Action<bool> set, Action extra, bool execute) {
			element.IsChecked = get();

			element.Click += (e, a) => {
				set(!get());

				if (extra != null)
					extra();
			};

			if (execute) {
				if (extra != null)
					extra();
			}
		}
	}

	public class RadioButtonBinder : BinderAbstract<RadioButton, bool> {
		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		/// <param name="execute">Executes the action after this method.</param>
		public override void Bind(RadioButton element, Func<bool> get, Action<bool> set, Action extra, bool execute) {
			element.IsChecked = get();

			element.Click += (e, a) => {
				set(!get());

				if (extra != null)
					extra();
			};

			if (execute) {
				if (extra != null)
					extra();
			}
		}
	}

	public class MenuItemBinder : BinderAbstract<MenuItem, bool> {
		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		/// <param name="execute">Executes the action after this method.</param>
		public override void Bind(MenuItem element, Func<bool> get, Action<bool> set, Action extra, bool execute) {
			element.IsChecked = get();
			element.IsCheckable = true;

			element.Checked += (e, a) => {
				set(true);

				if (extra != null)
					extra();
			};

			element.Unchecked += (e, a) => {
				set(false);

				if (extra != null)
					extra();
			};

			if (execute) {
				if (extra != null)
					extra();
			}
		}
	}

	public class TkMenuItemBinder : BinderAbstract<TkMenuItem, bool> {
		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		/// <param name="execute">Executes the action after this method.</param>
		public override void Bind(TkMenuItem element, Func<bool> get, Action<bool> set, Action extra, bool execute) {
			element.IsChecked = get();
			element.IsCheckable = true;

			element.Checked += (e, a) => {
				set(true);

				if (extra != null)
					extra();
			};

			element.Unchecked += (e, a) => {
				set(false);

				if (extra != null)
					extra();
			};

			if (execute) {
				if (extra != null)
					extra();
			}
		}
	}

	public class ComboBoxBinder : BinderAbstract<ComboBox, int> {
		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		/// <param name="execute">Executes the action after this method.</param>
		public override void Bind(ComboBox element, Func<int> get, Action<int> set, Action extra, bool execute) {
			Action action = delegate {
				if (get() >= element.Items.Count)
					set(0);

				element.SelectedIndex = get();
				element.SelectionChanged += (e, a) => {
					set(element.SelectedIndex);
					if (extra != null)
						extra();
				};

				if (execute) {
					if (extra != null)
						extra();
				}
			};

			if (element.IsLoaded) {
				action();
			}
			else {
				element.Loaded += delegate { action(); };
			}
		}
	}

	public class PathBrowserBinder : BinderAbstract<PathBrowser, string> {
		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		/// <param name="execute">Executes the action after this method.</param>
		public override void Bind(PathBrowser element, Func<string> get, Action<string> set, Action extra, bool execute) {
			element.Text = get();

			element.TextBox.TextChanged += delegate {
				set(element.Text);

				if (extra != null)
					extra();
			};

			if (execute) {
				if (extra != null)
					extra();
			}
		}
	}

	public class TextBoxBinder<TValue> : BinderAbstract<TextBox, TValue> {
		private readonly Func<string, TValue> _converter;

		public TextBoxBinder(Func<string, TValue> converter) {
			_converter = converter;
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		/// <param name="execute">Executes the action after this method.</param>
		public override void Bind(TextBox element, Func<TValue> get, Action<TValue> set, Action extra, bool execute) {
			element.Text = get().ToString();

			element.TextChanged += delegate {
				set(_converter(element.Text));

				if (extra != null)
					extra();
			};

			if (execute) {
				if (extra != null)
					extra();
			}
		}
	}

	public class PasswordBoxBinder<TValue> : BinderAbstract<PasswordBox, TValue> {
		private readonly Func<string, TValue> _converter;

		public PasswordBoxBinder(Func<string, TValue> converter) {
			_converter = converter;
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		/// <param name="execute">Executes the action after this method.</param>
		public override void Bind(PasswordBox element, Func<TValue> get, Action<TValue> set, Action extra, bool execute) {
			element.Password = get().ToString();

			element.PasswordChanged += delegate {
				set(_converter(element.Password));

				if (extra != null)
					extra();
			};

			if (execute) {
				if (extra != null)
					extra();
			}
		}
	}

	public abstract class BinderAbstract<TElement, TValue> : BinderBase {
		protected BinderAbstract() {
			Binder.AddBinder<TElement, TValue>(this);
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The check box.</param>
		/// <param name="get">The get.</param>
		public void Bind(TElement element, Expression<Func<TValue>> get) {
			Bind(element, get, null);
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The check box.</param>
		/// <param name="get">The get.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		public void Bind(TElement element, Expression<Func<TValue>> get, Action extra) {
			Bind(element, get, extra, false);
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The check box.</param>
		/// <param name="get">The get.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		/// <param name="execute">Executes the action.</param>
		public void Bind(TElement element, Expression<Func<TValue>> get, Action extra, bool execute) {
			var body = (MemberExpression)get.Body;
			var pi = (PropertyInfo)body.Member;

			Bind(element, get.Compile(), v => pi.SetValue(null, v, null), extra, execute);
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		public void Bind(TElement element, Func<TValue> get, Action<TValue> set) {
			Bind(element, get, set, null, false);
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="element">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		public void Bind(TElement element, Func<TValue> get, Action<TValue> set, Action extra) {
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
		public abstract void Bind(TElement element, Func<TValue> get, Action<TValue> set, Action extra, bool execute);
	}
}