using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using ErrorManager;

namespace TokeiLibrary.WPF {
	public struct EncodingDefinition {
		public int Encoding;
		public string Name;
	}

	public static class EncodingSelector {
		public static void CreateSelector(ComboBox box, 
			Utilities.TypeSetting<int> codepageSetting, 
			Utilities.TypeSetting<int> boxIndexSetting, 
			Utilities.TypeSetting<Encoding> encodingSetting, 
			EncodingDefinition[] definitions) {
			var items = new ObservableCollection<string>(definitions.Select(p => p.Name));
			box.ItemsSource = items;

			Func<int, bool> setEncoding = encoding => {
				try {
					encodingSetting.Set(Encoding.GetEncoding(encoding));
					codepageSetting.Set(encoding);
					return true;
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err.Message, ErrorLevel.Critical);
					return false;
				}
			};

			if (boxIndexSetting != null) {
				if (boxIndexSetting.Get() == box.Items.Count - 1) {
					items[box.Items.Count - 1] = codepageSetting.Get() + "...";
				}

				box.SelectedIndex = boxIndexSetting.Get();
			}
			else {
				var current = codepageSetting.Get();
				var found = false;

				for (int i = 0; i < definitions.Length; i++) {
					if (definitions[i].Encoding == current && definitions[i].Encoding > -1) {
						box.SelectedIndex = i;
						found = true;
						break;
					}
				}

				if (!found) {
					items[box.Items.Count - 1] = current + "...";
					box.SelectedIndex = definitions.Length - 1;
				}
			}

			box.SelectionChanged += new SelectionChangedEventHandler(_box_SelectionChanged);
			setEncoding(codepageSetting.Get());
		}

		private static void _box_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			//object oldSelected = null;
			//bool cancel = false;
			//
			//if (e.RemovedItems.Count > 0)
			//	oldSelected = e.RemovedItems[0];


		}
	}
}
