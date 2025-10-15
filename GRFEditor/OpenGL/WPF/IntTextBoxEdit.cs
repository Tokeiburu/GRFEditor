using System;

namespace GRFEditor.OpenGL.WPF {
    public class IntTextBoxEdit : AdvancedTextBox {
		public int MinValue = Int32.MinValue;
		public int MaxValue = Int32.MaxValue;
		public float DeltaMultiplier = 1;

		public IntTextBoxEdit() {
			Init(p => {
				_previewMid.Text = p;
			});
		}

		protected override void OnMouseValueChanged(float deltax, float deltay, bool addCommand) {
			var oldValue = AddCommand;

			try {
				AddCommand = addCommand;

                // addCommand == true, it's from clicking right/left arrows
                // addCommand == false, it's from the mouse slider
                if (!addCommand)
					deltax *= DeltaMultiplier;

				int value = GetInt() + (int)deltax;

				if (value < MinValue)
					value = MinValue;
				if (value > MaxValue)
					value = MaxValue;

				TextNoEvent = (value) + "";
				OnTextChanged(null, addCommand);
				base.OnMouseValueChanged(deltax, deltay, addCommand);
			}
			finally {
				AddCommand = oldValue;
			}
		}
	}
}
