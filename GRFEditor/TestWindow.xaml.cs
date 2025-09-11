using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.OpenGL.MapComponents;
using TokeiLibrary;
using Utilities;

namespace GRFEditor {
	/// <summary>
	/// Interaction logic for TestWindow.xaml
	/// </summary>
	public partial class TestWindow : Window {
		private readonly Stopwatch _watchRenderStart = new Stopwatch();
		private long _currentTick;
		private long _previousTick;
		private int _frameCount;
		private long _fpsRefreshTimer;
		private long _fpsUpdateFrequency = 200;

		public TestWindow() {
			InitializeComponent();


			//GrfThread.Start(delegate {
			//	while (true) {
			//		Thread.Sleep(100);
			//
			//		this.Dispatch(p => {
			//			Console.WriteLine("Keyboard: " + Keyboard.FocusedElement + "\tFocus: " + FocusManager.GetFocusedElement(this));
			//		});
			//	}
			//});

			_lastMeasureTime = DateTime.Now;
			_testControl._viewport.SetFpsTextBlock(_tbFps);
			_testControl._viewport.RenderOptions.ShowFps = true;
			_testControl._viewport.RenderOptions.FpsCap = -1;
			GrfEditorConfiguration.Resources = new GrfEditorConfiguration.GrfResources(null);
			Rsm.ForceShadeType = -1;
			var mapName = @"data\1@4cdn";
			_testControl._viewport.Loader.AddRequest(new RendererLoadRequest { IsMap = true, Resource = mapName, CancelRequired = () => false, Context = _testControl._viewport });

			//GrfThread.Start(_test);
			_watchRenderStart.Start();
			//DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Input);
			//
			//timer.Interval = TimeSpan.FromMilliseconds(1);
			//timer.Tick += (s, e) => _test();
			//timer.Start();
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e) {
			ApplicationManager.Shutdown();
		}

		private void _test() {
			while (true) {
				//if (DateTime.Now.Subtract(_lastMeasureTime) > TimeSpan.FromSeconds(1)) {
				//	this.Title = _frameCount + " fps";
				//	_frameCount = 0;
				//	_lastMeasureTime = DateTime.Now;
				//}

				//while (true) {
				long renderTick = _watchRenderStart.ElapsedMilliseconds;
				Z.F();
				//_render();
				this.Dispatch(p => _render());
				var interval = _watchRenderStart.ElapsedMilliseconds - renderTick;
			}

			//}
		}

		private DateTime _lastMeasureTime;

		private void _render() {
			_currentTick = _watchRenderStart.ElapsedMilliseconds;
			FrameRenderTime = _currentTick - _previousTick;

			// render lol
			Z.F();

			_frameCount++;
			_fpsRefreshTimer -= FrameRenderTime;
			
			if (_fpsRefreshTimer <= 0) {
				if (_tbFps != null) {
					int fps = (int)Math.Ceiling(_frameCount * 1000f / (_fpsUpdateFrequency - _fpsRefreshTimer));
					_tbFps.Text = fps + "";
				}
			
				_frameCount = 0;
				_fpsRefreshTimer = _fpsUpdateFrequency;
			}

			//_primary.SwapBuffers();
			_previousTick = _currentTick;
		}

		public long FrameRenderTime { get; set; }
	}
}
