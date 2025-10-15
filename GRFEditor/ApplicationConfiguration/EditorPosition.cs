using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace GRFEditor.ApplicationConfiguration {
	public class EditorPosition {
		public bool IsSet { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }
		public double X { get; set; }
		public double Y { get; set; }
		public double Splitter1 { get; set; }
		public double Splitter2 { get; set; }

		public void FromString(string config) {
			var items = Methods.StringToList(config, ';');
			int i = 0;

			if (items.Count == 0)
				return;

			IsSet = FormatConverters.BooleanConverter(items[i++]);
			Width = FormatConverters.DoubleConverterNoThrow(items[i++]);
			Height = FormatConverters.DoubleConverterNoThrow(items[i++]);
			X = FormatConverters.DoubleConverterNoThrow(items[i++]);
			Y = FormatConverters.DoubleConverterNoThrow(items[i++]);
			Splitter1 = FormatConverters.DoubleConverterNoThrow(items[i++]);
			Splitter2 = FormatConverters.DoubleConverterNoThrow(items[i++]);
		}

		public override string ToString() {
			if (!IsSet)
				return "";

			List<string> values = new List<string>();
			values.Add(IsSet + "");
			values.Add(Width + "");
			values.Add(Height + "");
			values.Add(X + "");
			values.Add(Y + "");
			values.Add(Splitter1 + "");
			values.Add(Splitter2 + "");

			return Methods.Aggregate(values, ";");
		}

		public void Load(EditorMainWindow editor) {
			FromString(GrfEditorConfiguration.EditorSavedPositions);

			if (!IsSet || !GrfEditorConfiguration.SaveEditorPosition)
				return;

			editor.Width = Width;
			editor.Height = Height;
			editor.Left = X;
			editor.Top = Y;
			editor._gridTreeView.ColumnDefinitions[0].Width = new System.Windows.GridLength(Splitter1);
			editor._primaryGrid.ColumnDefinitions[0].Width = new System.Windows.GridLength(Splitter2);
		}

		public void Save(EditorMainWindow editor) {
			if (editor.WindowState == System.Windows.WindowState.Maximized)
				return;

			IsSet = true;
			Width = editor.Width;
			Height = editor.Height;
			X = editor.Left;
			Y = editor.Top;
			Splitter1 = editor._gridTreeView.ColumnDefinitions[0].ActualWidth;
			Splitter2 = editor._primaryGrid.ColumnDefinitions[0].ActualWidth;
			Save();
		}

		public void Save() {
			GrfEditorConfiguration.EditorSavedPositions = ToString();
		}
	}
}
