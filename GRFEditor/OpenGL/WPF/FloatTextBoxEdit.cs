using System;

namespace GRFEditor.OpenGL.WPF {
	public class FloatTextBoxEdit : AdvancedTextBox {
		public float Multiplier = 0.1f;
		public string DisplayFormat = "{0:0.00}";

		public FloatTextBoxEdit() {
			_previewLeft.Visibility = System.Windows.Visibility.Collapsed;
			_previewRight.Visibility = System.Windows.Visibility.Collapsed;

			Init(p => {
				float value;
				if (p == "")
					p = "0";

				if (float.TryParse(p, out value)) {
					_previewMid.Text = String.Format(DisplayFormat, value);
				}
				else {
					_previewMid.Text = p;
				}
			});
		}

		protected override void OnMouseValueChanged(float deltax, float deltay, bool addCommand) {
			var oldValue = AddCommand;

			try {
				AddCommand = addCommand;
				this.Text = (GetFloat() + deltax * Multiplier) + "";
				OnTextChanged(null, addCommand);
				base.OnMouseValueChanged(deltax, deltay, addCommand);
			}
			finally {
				AddCommand = oldValue;
			}
		}
	}
}
