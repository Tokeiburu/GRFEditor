using System;
using System.Threading;
using GRF.FileFormats.GatFormat;
using GRF.FileFormats.RswFormat;
using GRFEditor.OpenGL.MapRenderers;

namespace GRFEditor.OpenGL.MapComponents {
	public class RendererLoadRequest {
		public bool IsMap { get; set; }
		public bool Preloaded { get; set; }
		public string Resource { get; set; }
		public Rsm Rsm { get; set; }
		public Func<bool> CancelRequired { get; set; }
		public Rsw Rsw { get; set; }
		public Gnd Gnd { get; set; }
		public Gat Gat { get; set; }
		public GndRenderer GndRenderer { get; set; }
		public MapRenderer MapRenderer { get; set; }
		public object Context { get; set; }
		public bool ClearOnly { get; set; }
	}

	public class RendererLoader {
		public RendererLoadRequest RendererLoadRequest;
		private readonly object _loadLock = new object();
		private readonly AutoResetEvent _are = new AutoResetEvent(false);
		public Action ClearFunction;
		public Action<RendererLoadRequest> LoadFunction;
		private bool _isEnabled = true;

		public delegate void DataLoadedEventHandler(RendererLoadRequest request);

		public event DataLoadedEventHandler Loaded;

		public virtual void OnLoaded(RendererLoadRequest request) {
			DataLoadedEventHandler handler = Loaded;
			if (handler != null) handler(request);
		}

		public bool CancelLoad { get; set; }

		public RendererLoader() {
			new Thread(_loadThread) { Name = "GrfEditor - Map load thread" }.Start();
		}

		private void _loadThread() {
			while (_isEnabled) {
				RendererLoadRequest request;

				lock (_loadLock) {
					request = RendererLoadRequest;
					RendererLoadRequest = null;
				}

				if (request == null) {
					Pause();
					continue;
				}

				CancelLoad = false;

				try {
					ClearFunction();

					if (!request.ClearOnly) {
						LoadFunction(request);
					}
				}
				catch {
				}

				if (!_isEnabled)
					break;
			}
		}

		public void AddRequest(RendererLoadRequest request) {
			lock (_loadLock) {
				RendererLoadRequest = request;
			}

			CancelLoad = true;
			Resume();
		}

		protected void Pause() {
			_are.WaitOne();
		}

		protected void Resume() {
			_are.Set();
		}

		public void ExitThreads() {
			_isEnabled = false;
			Resume();
		}
	}
}
