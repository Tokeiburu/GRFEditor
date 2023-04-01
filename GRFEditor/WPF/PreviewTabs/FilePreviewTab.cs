using System;
using System.Threading;
using System.Windows.Controls;
using ErrorManager;
using GRF.Core;

namespace GRFEditor.WPF.PreviewTabs {
	public class FilePreviewTab : UserControl, IPreviewTab {
		protected readonly object _lock = new object();
		protected FileEntry _entry;
		protected GrfHolder _grfData;
		protected Action _isInvisibleResult;
		protected FileEntry _oldEntry;
		protected bool _requiresSTA = false;

		public FilePreviewTab() {
		}

		protected FilePreviewTab(bool requiresSTA = false) {
			_requiresSTA = requiresSTA;
		}

		protected Func<bool> _isCancelRequired { get; private set; }

		public void Update(bool forceUpdate) {
			if (forceUpdate) {
				_oldEntry = null;
			}

			Update();
		}

		public void Update() {
			if (_oldEntry == _entry)
				return;

			if (_isInvisibleResult != null) {
				Dispatcher.Invoke(new Action(delegate {
					if (!IsVisible && _isInvisibleResult != null)
						_isInvisibleResult();
				}));
			}

			Thread thread = new Thread(() => _baseLoad(_entry)) { Name = "GrfEditor - IPreview base loading thread" };

			if (_requiresSTA) {
				thread.SetApartmentState(ApartmentState.STA);
			}

			thread.Start();
		}

		public void Load(GrfHolder grfData, FileEntry entry) {
			_entry = entry;
			_grfData = grfData;

			if (IsVisible) {
				Update();
			}
		}

		protected void _baseLoad(FileEntry entry) {
			try {
				lock (_lock) {
					if (_entry == null) return;

					_isCancelRequired = new Func<bool>(() => entry != _entry);

					if (_isCancelRequired()) return;

					_load(entry);

					_oldEntry = entry;
				}
			}
			catch (ObjectDisposedException err) {
				ErrorHandler.HandleException("If you're receiving this error while the GRF is saving, it is because the file is temporary closed.", err);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		protected virtual void _load(FileEntry entry) {
		}

		public void InvalidateOnReload(GrfHolder grf) {
			lock (_lock) {
				_oldEntry = null;

				if (grf.FileTable.ContainsFile(_entry.RelativePath))
					Load(grf, grf.FileTable[_entry.RelativePath]);
			}
		}
	}
}