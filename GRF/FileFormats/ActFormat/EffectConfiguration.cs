using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GRF.Image;
using Utilities;

namespace GRF.FileFormats.ActFormat {
	public class EffectConfiguration {
		public static ConfigAsker ConfigAsker { get; set; }
		public static Action<EffectConfiguration, Act, int> DisplayAction { get; set; }
		public static int SkipAndRememberInput { get; set; }

		public class EffectProperty {
			public object Name { get; set; }
			public object DefValue { get; set; }
			public object MinValue { get; set; }
			public object MaxValue { get; set; }
			public object Value { get; set; }
			public Type Type { get; set; }
			public string SettingName { get; set; }
		}

		public Dictionary<string, EffectProperty> Properties = new Dictionary<string, EffectProperty>();
		public bool Preview { get; set; }
		public bool InvalidateSprite { get; set; }
		public Action<Act> EffectFunc;
		public static bool Displayed { get; set; }
		public string ParentType { get; set; }

		public EffectConfiguration(string parent) {
			ParentType = parent;
		}

		public void AddProperty<T>(string name, T defValue, T minValue, T maxValue) {
			var property = new EffectProperty {
				Name = name,
				DefValue = defValue,
				MinValue = minValue,
				MaxValue = maxValue,
				Type = typeof(T),
				SettingName = "ActEditor - Effect - " + ParentType + " - " + name
			};

			if (ConfigAsker.ContainsKey(property.SettingName)) {
				if (typeof(T) == typeof(GrfColor)) {
					property.Value = new GrfColor(ConfigAsker[property.SettingName]);
				}
				else if (typeof(T) == typeof(float)) {
					property.Value = FormatConverters.SingleConverter(ConfigAsker[property.SettingName]);
				}
				else if (typeof(T) == typeof(int)) {
					property.Value = FormatConverters.IntConverter(ConfigAsker[property.SettingName]);
				}
				else {
					property.Value = ConfigAsker[property.SettingName];
				}
			}
			else {
				property.Value = defValue;
			}

			Properties[name] = property;
		}

		public T GetProperty<T>(string name) {
			return (T)Properties[name].Value;
		}

		public void Apply(Action<Act> action) {
			EffectFunc = action;
		}

		public void Display(Act act, int actionIndex) {
			if (ConfigAsker == null || DisplayAction == null)
				throw new Exception("The EffectConfiguration must set the ConfigAsker and DisplayAction static functions before usage.");

			DisplayAction(this, act, actionIndex);
		}
	}
}
