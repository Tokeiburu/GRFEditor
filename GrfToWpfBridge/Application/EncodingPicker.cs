using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Controls;
using ErrorManager;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities;
using Utilities.Services;

namespace GrfToWpfBridge.Application {
	public class EncodingArgs : EventArgs {
		public EncodingArgs(Encoding encoding) {
			Encoding = encoding;
		}

		public Encoding Encoding { get; private set; }
		public bool Cancel { get; set; }
	}

	public class EncodingPicker : ComboBox {
		#region Delegates

		public delegate void EncodingEventHandler(object sender, EncodingArgs args);

		#endregion

		private TypeSetting<int> _encodingCodePage;
		private TypeSetting<Encoding> _encodingGetter;
		private RangeObservableCollection<EncodingView> _encodings = new RangeObservableCollection<EncodingView>();

		public Encoding CurrentEncoding {
			get { return Encoding.GetEncoding(_encodingCodePage.Get()); }
		}

		public event EncodingEventHandler EncodingChanged;

		public void OnEncodingChanged(EncodingArgs args) {
			EncodingEventHandler handler = EncodingChanged;
			if (handler != null) handler(this, args);
		}

		public void Init(List<EncodingView> encodings, TypeSetting<int> encodingCodePage, TypeSetting<Encoding> encodingGetter) {
			_encodings = new RangeObservableCollection<EncodingView>(encodings ?? EncodingService.GetKnownEncodings());
			_encodingCodePage = encodingCodePage;
			_encodingGetter = encodingGetter;

			ItemsSource = _encodings;

			SelectedIndex = _findSelectedIndex(_encodingCodePage.Get());
			_update(false);

			SelectionChanged += _encodingPicker_SelectionChanged;
		}

		private void _encodingPicker_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			_update(true);
		}

		private void _reset(bool userInput) {
			SelectedIndex = 0;

			if (!userInput) {
				_update(false);
			}
		}

		private int _findSelectedIndex(int currentEncoding) {
			if (!EncodingService.EncodingExists(currentEncoding))
				currentEncoding = 1252;

			int currentIndex = -1;

			for (int index = 0; index < _encodings.Count; index++) {
				var encoding = _encodings[index];

				if (encoding.Encoding == null) {
					if (currentIndex < 0) {
						currentIndex = index;
					}

					if (currentIndex == index) {
						encoding.FriendlyName = currentEncoding + "...";
					}
					else {
						encoding.FriendlyName = "Other...";
					}
				}
				else if (encoding.Encoding.CodePage == currentEncoding) {
					currentIndex = index;
				}
			}

			if (currentIndex < 0)
				currentIndex = 0;

			return currentIndex;
		}

		private void _update(bool userInput) {
			int oldEncoding = _encodingCodePage.Get();

			if (!EncodingService.EncodingExists(oldEncoding))
				oldEncoding = _encodings[0].Encoding.CodePage;

			if (_encodings[SelectedIndex].Encoding == null) {
				try {
					int newCodePage = -1;

					if (userInput) {
						InputDialog dialog = WindowProvider.ShowWindow<InputDialog>(
							new InputDialog(
								"Using an unsupported encoding may cause unexpected results.\n" +
								"Enter the codepage number for the encoding :",
								"Encoding",
								oldEncoding.ToString(CultureInfo.InvariantCulture)),
							WpfUtilities.TopWindow);

						if (dialog.DialogResult == true) {
							if (EncodingService.EncodingExists(dialog.Input)) {
								newCodePage = Int32.Parse(dialog.Input);

								_encodings[SelectedIndex].FriendlyName = newCodePage + "...";
							}
						}

						if (newCodePage < 0) {
							SelectedIndex = _findSelectedIndex(oldEncoding);
							return;
						}
					}
					else {
						newCodePage = _encodingCodePage.Get();
					}

					if (!EncodingService.EncodingExists(newCodePage)) {
						newCodePage = -1;
					}

					if (newCodePage < 0) {
						if (userInput) {
							ErrorHandler.HandleException("The encoding specified is not supported.");
						}

						SelectedIndex = 0;
						_update(false);
						return;
					}

					_encodingGetter.Set(Encoding.GetEncoding(newCodePage));
					_encodingCodePage.Set(newCodePage);
					var args = new EncodingArgs(_encodingGetter.Get());
					OnEncodingChanged(args);

					if (args.Cancel) {
						try {
							SelectionChanged -= _encodingPicker_SelectionChanged;
							SelectedIndex = _findSelectedIndex(oldEncoding);
							_encodingGetter.Set(Encoding.GetEncoding(oldEncoding));
							_encodingCodePage.Set(oldEncoding);
						}
						finally {
							SelectionChanged += _encodingPicker_SelectionChanged;
						}
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
					_reset(userInput);
				}
			}
			else {
				// Direct assign
				try {
					_encodingGetter.Set(_encodings[SelectedIndex].Encoding);
					_encodingCodePage.Set(_encodings[SelectedIndex].Encoding.CodePage);
					var args = new EncodingArgs(_encodingGetter.Get());
					OnEncodingChanged(args);

					if (args.Cancel) {
						try {
							SelectionChanged -= _encodingPicker_SelectionChanged;
							SelectedIndex = _findSelectedIndex(oldEncoding);
							_encodingGetter.Set(Encoding.GetEncoding(oldEncoding));
							_encodingCodePage.Set(oldEncoding);
						}
						finally {
							SelectionChanged += _encodingPicker_SelectionChanged;
						}
					}
				}
				catch {
					ErrorHandler.HandleException("Couldn't load the encoding, it has been reseted to the default value.");
					_reset(userInput);
				}
			}
		}

		public void Refresh() {
			try {
				try {
					SelectionChanged -= _encodingPicker_SelectionChanged;
					SelectedIndex = _findSelectedIndex(_encodingCodePage.Get());
				}
				finally {
					SelectionChanged += _encodingPicker_SelectionChanged;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}