using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GRF.Threading;
using GRFEditor.Core.Avalon;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.WPF;
using GRFEditor.WPF.PreviewTabs;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using Utilities;

namespace GRFEditor.OpenGL.WPF {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class OpenGLDebugDialog : TkWindow {
		public class OpenGLLoggerThread : PausableThread {
			public List<string> Messages = new List<string>();
			private readonly object _lock = new object();
			private OpenGLDebugDialog _dialog;
			public bool IsTerminated { get; set; }

			public void Start(OpenGLDebugDialog dialog) {
				_dialog = dialog;
				GrfThread.Start(_start, "");
			}

			private void _start() {
				while (true) {
					bool toPause = false;

					lock (_lock) {
						toPause = Messages.Count == 0;
					}

					if (toPause)
						IsPaused = true;

					if (IsPaused)
						Pause();

					if (IsTerminated)
						return;

					List<string> messages = new List<string>();

					lock (_lock) {
						messages.AddRange(Messages);
						Messages.Clear();
					}

					bool clear = false;
					int idx = messages.IndexOf(null);

					if (idx > -1) {
						clear = true;
						messages = messages.Skip(idx).ToList();
					}

					if (clear) {
						_dialog.Dispatch(delegate {
							_dialog._textEditor.Text = "";
						});
					}

					_dialog.Log = Methods.Aggregate(messages, "");
					Thread.Sleep(100);
				}
			}

			public void Add(string message) {
				lock (_lock) {
					if (message != null)
						message += "\r\n";

					Messages.Add(message);
				}

				Resume();
			}

			public void Terminate() {
				IsTerminated = true;
				IsPaused = false;
			}
		}

		private readonly OpenGLLoggerThread _openGLThread = new OpenGLLoggerThread();

		public OpenGLDebugDialog()
			: base("OpenGL logger...", "warning16.png", SizeToContent.Manual, ResizeMode.CanResize) {
			InitializeComponent();
			GLHelper.LogEnabled = true;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			Owner = WpfUtilities.TopWindow;

			AvalonHelper.Load(_textEditor);
			AvalonHelper.SetSyntax(_textEditor, "DebugDb");

			Log = "OpenGL: Logger Started...\r\n";

			_openGLThread.Start(this);

			GLHelper.Log += delegate(object sender, string message) {
				_openGLThread.Add(message);
			};

			Dispatcher.ShutdownStarted += delegate {
				_openGLThread.Terminate();
			};
		}

		public string Log {
			set {
				_textEditor.Dispatch(delegate {
					_textEditor.Text += value;
					_textEditor.ScrollToEnd();
				});
			}
		}

		protected override void OnClosing(CancelEventArgs e) {
			_openGLThread.Terminate();
			Log = "GRF Editor: Cleaning memory...";
			GLHelper.LogEnabled = false;
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			base.OnClosing(e);
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape)
				Close();
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _buttonMemory_Click(object sender, RoutedEventArgs e) {
			Log = "//---------------------------\r\n";
			Log = "//   Memory usage\r\n";
			Log = "//---------------------------\r\n";

			Log = "- Found " + OpenGLMemoryManager._managers.Count + " OpenGL context(s)\r\n";

			foreach (var managerGroup in OpenGLMemoryManager._managers) {
				if (!(managerGroup.Key is OpenGLViewport))
					continue;

				var viewport = (OpenGLViewport)managerGroup.Key;
				var manager = managerGroup.Value;

				Log = "- Context: " + FindParentControl(viewport) + " (" + viewport.GetHashCode() + ")\r\n";
				Log = "  - Last request: \"" + (viewport._request == null ? "[null]" : viewport._request.Resource) + "\"\r\n";
				Log = "  - Texture requested: " + TextureManager.ContextBufferedTextures[viewport].Sum(p => p.Value) + " / " + TextureManager.BufferedTextures.Sum(p => p.Value.Item2) + "\r\n";
				Log = "  - Unique texture: " + manager.TextureIds.Count + " / " + OpenGLMemoryManager._managers.Sum(p => p.Value.TextureIds.Count) + "\r\n";
				Log = "  - VAO: " + manager.VertexArrayObjects.Count + " / " + OpenGLMemoryManager._managers.Sum(p => p.Value.VertexArrayObjects.Count) + "\r\n";
				Log = "  - VBO: " + manager.VertexBufferObjects.Count + " / " + OpenGLMemoryManager._managers.Sum(p => p.Value.VertexBufferObjects.Count) + "\r\n";
				Log = "  - Renderers: " + viewport.Renderers.Count + "\r\n";
				Log = "  - State - GLControl loaded: " + viewport._glControlReady + "\r\n";
				Log = "  - State - Closing: " + !viewport._isRunning + "\r\n";
				Log = "  - State - Rendering (render thread enabled): " + viewport._renderThreadEnabled + "\r\n";
				Log = "  - RenderOptions.FpsCap: " + viewport.RenderOptions.FpsCap + "\r\n";
				Log = "  - RenderOptions.AnimateMap: " + viewport.RenderOptions.AnimateMap + "\r\n";
				Log = "  - RenderOptions.EnableFaceCulling: " + viewport.RenderOptions.EnableFaceCulling + "\r\n";
				Log = "  - RenderOptions.LubEffect: " + viewport.RenderOptions.LubEffect + "\r\n";
				Log = "  - RenderOptions.ForceShader: " + viewport.RenderOptions.ForceShader + "\r\n";
			}

			Log = " \r\n";
			Log = "- All textures info:\r\n";

			foreach (var textureInfo in TextureManager.BufferedTextures) {
				var texture = textureInfo.Value.Item1;

				if (texture.Id == 0) {
					Log = "  - \"" + textureInfo.Key + "\", Instance: " + textureInfo.Value.Item2 + ", not loaded\r\n";
				}
				else {
					Log = "  - \"" + textureInfo.Key + "\", Instance: " + textureInfo.Value.Item2 + ", ID: " + texture.Id + ", Size: " + Methods.FileSizeToString(texture.Size) + "\r\n";
				}
			}

			Log = " \r\n";
			Log = "- Texture details for each context:\r\n";

			foreach (var contextTextureInfo in TextureManager.ContextBufferedTextures) {
				if (!(contextTextureInfo.Key is OpenGLViewport))
					continue;

				var viewport = (OpenGLViewport)contextTextureInfo.Key;
				var textureDico = contextTextureInfo.Value;

				Log = "- Context: " + FindParentControl(viewport) + " (" + viewport.GetHashCode() + ")\r\n";

				foreach (var textureInfo in textureDico) {
					Log = "  - \"" + textureInfo.Key + "\", Instance: " + textureInfo.Value + (TextureManager.BufferedTextures[textureInfo.Key].Item1.Id == 0 ? ", not loaded" : ", ID: " + TextureManager.BufferedTextures[textureInfo.Key].Item1.Id) + "\r\n";
				}
			}

			Log = "//---------------------------\r\n";
		}

		private string FindParentControl(OpenGLViewport viewport) {
			try {
				FrameworkElement parent = viewport.Parent as FrameworkElement;

				while (parent is Grid || parent is QuickPreview) {
					parent = parent.Parent as FrameworkElement;

					if (parent is FilePreviewTab || parent is Window)
						return parent.GetType().ToString();
				}

				if (parent == null)
					return viewport._editorWindow.GetType().ToString();

				return parent.GetType().ToString();
			}
			catch {
				return viewport._editorWindow.GetType().ToString();
			}
		}

		private void _buttonClear_Click(object sender, RoutedEventArgs e) {
			_textEditor.Text = "";
		}
	}
}
