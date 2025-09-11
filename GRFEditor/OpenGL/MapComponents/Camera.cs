using System;
using System.Diagnostics;
using System.Windows;
using GRFEditor.OpenGL.MapRenderers;
using GRFEditor.OpenGL.WPF;
using OpenTK;
using Utilities;

namespace GRFEditor.OpenGL.MapComponents {
	public enum CameraMode {
		Minimap,
		Client,
		PerspectiveOpenGL,
		PerspectiveDirectX,
		Orthographic,
	}

	public class Camera {
		public double AngleX_Degree { get; set; }
		public double AngleY_Degree { get; set; }
		public float ZNear { get; set; }
		public float ZFar { get; set; }
		public Vector3 LookAt = new Vector3(0, 0, 0);
		public Vector3 Position = new Vector3(50, 20, 0f);
		public double Distance { get; set; }
		public CameraMode Mode { get; set; }

		private OpenGLViewport _viewport;

		public OpenGLViewport Viewport {
			set { _viewport = value; }
		}

		public double DistanceMultiplier_Map {
			get {
				switch(Mode) {
					case CameraMode.PerspectiveDirectX:
						return 17;
					default:
						return 10;
				}
			}
		}

		public double DistanceMultiplier_Rsm {
			get {
				switch (Mode) {
					case CameraMode.PerspectiveDirectX:
						return 4;
					default:
						return 3;
				}
			}
		}

		public double TranslateMultiplier {
			get {
				switch (Mode) {
					case CameraMode.PerspectiveDirectX:
						return 0.6d;
					default:
						return 1;
				}
			}
		}

		public Vector2 DeltaEye;
		public Vector3 DeltaLookAt;
		public double DeltaDistance;
		public float MaxDistance = 3500f;

		private double _renderTimePerFrameCamera;

		public Camera(OpenGLViewport viewport) {
			Mode = CameraMode.PerspectiveOpenGL;
			//Mode = CameraMode.PerspectiveDirectX;

			AngleY_Degree = 20;
			Distance = 20;
			_updateWatch.Start();
			_viewport = viewport;
			ZFar = 99999f;
		}

		public Matrix4 GetProjectionMatrix() {
			if (Distance < 50) {
				ZNear = 1f;
			}
			else if (Distance < 100) {
				ZNear = 5f;
			}
			else if (Distance < 1500) {
				ZNear = 10f;
			}
			else {
				ZNear = 100f;
			}

			if (_viewport.RenderOptions.MinimapMode) {
				float ratio = _viewport._primary.Width / (float)_viewport._primary.Height;
				float w = (float)Distance / 2f * ratio;
				float h = (float)Distance / 2f * 1;

				return Matrix4.CreateOrthographicOffCenter(-w, w, -h, h, -5000f, 5000f);
			}

			float fovy = GLHelper.ToRad(_viewport.RenderOptions.UseClientPov ? 15 : 45);

			switch (Mode) {
				case CameraMode.PerspectiveDirectX:
					float aspectRatio = _viewport._primary.Width / (float)(_viewport._primary.Height);
					float num = (float)Math.Tan(fovy / 2.0);
					float m00 = 1.0f / num;
					float m11 = aspectRatio / num;
					float m22 = -1.0f;
					float offsetZ = ZNear * m22;
					return new Matrix4(m00, 0.0f, 0.0f, 0.0f, 0.0f, m11, 0.0f, 0.0f, 0.0f, 0.0f, m22, -1.0f, 0.0f, 0.0f, offsetZ, 0.0f);
				case CameraMode.Orthographic: {
					float ratio = _viewport._primary.Width / (float)_viewport._primary.Height;
					float w = (float)Distance / 2f * ratio;
					float h = (float)Distance / 2f * 1;

					return Matrix4.CreateOrthographicOffCenter(-w, w, -h, h, -ZNear, ZFar);
					//float zoom = (float)Distance / _viewport._primary.Width;
					//float zoomX = zoom * ratio;
					//float zoomY = zoom * 1f;
					//return Matrix4.CreateOrthographic(_viewport._primary.Width * zoomX, _viewport._primary.Height * zoomY, ZNear, ZFar);
				}
				case CameraMode.PerspectiveOpenGL:
				default:
					return Matrix4.CreatePerspectiveFieldOfView(fovy, _viewport._primary.Width / (float)(_viewport._primary.Height), ZNear, ZFar);
			}
		}

		public Matrix4 GetViewMatrix() {
			return Matrix4.LookAt(Position, LookAt, new Vector3(0, 1, 0));
		}

		public string GetStringCopy() {
			return Position.X + ";" + Position.Y + ";" + Position.Z + ";" + LookAt.X + ";" + LookAt.Y + ";" + LookAt.Z + ";" + Distance + ";" + AngleX_Degree + ";" + AngleY_Degree;
		}

		public void Copy() {
			string copy = GetStringCopy();
			Clipboard.SetDataObject(copy);
			GLHelper.OnLog(() => "Saved camera settings: " + copy);
		}

		public void Paste(string paste = "") {
			try {
				string copy = paste != "" ? paste : Clipboard.GetText();
				var data = copy.Split(';');
				Position.X = FormatConverters.SingleConverterNoThrow(data[0]);
				Position.Y = FormatConverters.SingleConverterNoThrow(data[1]);
				Position.Z = FormatConverters.SingleConverterNoThrow(data[2]);
				LookAt.X = FormatConverters.SingleConverterNoThrow(data[3]);
				LookAt.Y = FormatConverters.SingleConverterNoThrow(data[4]);
				LookAt.Z = FormatConverters.SingleConverterNoThrow(data[5]);
				Distance = FormatConverters.SingleConverterNoThrow(data[6]);
				AngleX_Degree = FormatConverters.SingleConverterNoThrow(data[7]);
				AngleY_Degree = FormatConverters.SingleConverterNoThrow(data[8]);

				if (paste != "")
					GLHelper.OnLog(() => "Loaded camera settings: " + copy);
			}
			catch {
			}
		}

		private readonly Stopwatch _updateWatch = new Stopwatch();

		public void Update() {
			if (_viewport.IsRotatingCamera && _viewport.RotateCamera) {
				AngleX_Degree += GLHelper.ToDegree(_viewport.RenderOptions.RotateSpeed / (double)10000 * _viewport.FrameRenderTime);
			}

			if (_viewport.RenderOptions.SmoothCamera) {
				var elapsed = _updateWatch.ElapsedMilliseconds;
				_updateWatch.Reset();
				_updateWatch.Start();
				_renderTimePerFrameCamera += elapsed * 2;
				
				while (_renderTimePerFrameCamera > 2.5d) {
					AngleY_Degree += DeltaEye.Y;
					DeltaEye.Y *= 0.9f;
				
					if (Math.Abs(DeltaEye.Y) < 0.0001d)
						DeltaEye.Y = 0;
				
					AngleX_Degree -= GLHelper.ToDegree(DeltaEye.X);
					DeltaEye.X *= 0.9f;
				
					if (Math.Abs(DeltaEye.X) < 0.0001d)
						DeltaEye.X = 0;
				
					LookAt += DeltaLookAt;
					DeltaLookAt *= 0.8f;
				
					if (Math.Abs(DeltaLookAt.X) < 0.0001d)
						DeltaLookAt.X = 0;
				
					if (Math.Abs(DeltaLookAt.Y) < 0.0001d)
						DeltaLookAt.Y = 0;

					Distance += DeltaDistance;
					DeltaDistance *= 0.9f;
					Distance = GLHelper.Clamp(0f, MaxDistance, Distance);

					if (Math.Abs(DeltaDistance) < 0.0001d)
						DeltaDistance = 0;
				
					_renderTimePerFrameCamera -= 5d;
				}
			}

			float max = 89;

			if (_viewport.RenderOptions.MinimapMode)
				max = 89.9999f;

			AngleY_Degree = AngleY_Degree > max ? max : AngleY_Degree;
			AngleY_Degree = AngleY_Degree < -max ? -max : AngleY_Degree;

			double mult = 1;
			double rDistance = Distance;

			if (!_viewport.RenderOptions.MinimapMode && _viewport.RenderOptions.UseClientPov)
				mult = 3f;

			var angleX_Rad = GLHelper.ToRad(AngleX_Degree);
			double subDistance = mult * rDistance * Math.Cos(GLHelper.ToRad(AngleY_Degree));
			Position.X = (float)(subDistance * Math.Sin(angleX_Rad));
			Position.Y = (float)(mult * rDistance * Math.Sin(GLHelper.ToRad(AngleY_Degree)));
			Position.Z = (float)(subDistance * Math.Cos(angleX_Rad));
			Position += LookAt;

			MapRenderer.LookAt = LookAt;
		}

		public void CancelMovement() {
			DeltaEye = new Vector2(0);
			DeltaLookAt = new Vector3(0);
			DeltaDistance = 0;
		}

		public void TranslateY(double deltaY) {
			double distY = Distance * 0.0003 * deltaY * TranslateMultiplier;
			LookAt.Y += (float)distY;
			_viewport.IsRotatingCamera = false;
		}

		public void Zoom(float mult) {
			if (_viewport.RenderOptions.SmoothCamera) {
				DeltaDistance += ((Distance * mult) - Distance) * 0.1f;
			}
			else {	
				Distance *= mult;
				Distance = GLHelper.Clamp(0f, MaxDistance, Distance);
			}
		}

		public void RotateXY(double deltaX, double deltaY) {
			double distanceDelta = 1f;

			//if (_camera.Distance < 50) {
			//	distanceDelta = (1d - (200 - _camera.Distance) / 300f);
			//}
			//else
			if (Distance * TranslateMultiplier < 200) {
				distanceDelta = (1d - (200 - Distance) / 700f);
			}

			deltaX = GLHelper.ToRad(deltaX);

			if (_viewport.RenderOptions.SmoothCamera) {
				// Old settings
				//DeltaEye.X += (float)((deltaX * distanceDelta) / 10d) * (_viewport.RenderOptions.FpsCap > 0 ? 0.5f : 1f);
				//DeltaEye.Y += (float)((deltaY * distanceDelta) / 23d) * (_viewport.RenderOptions.FpsCap > 0 ? 0.75f : 1f);

				DeltaEye.X += (float)((deltaX * distanceDelta) / 20d);
				DeltaEye.Y += (float)((deltaY * distanceDelta) / 30d);
			}
			else {
				AngleX_Degree -= GLHelper.ToDegree(deltaX * distanceDelta);
				AngleY_Degree += deltaY * distanceDelta;
			}

			_viewport.IsRotatingCamera = false;
		}

		public void TranslateXZ(double deltaX, double deltaZ) {
			double distX = Distance * 0.0013 * deltaX;
			double distZ = Distance * 0.0013 * deltaZ;

			var angleX_Rad = GLHelper.ToRad(AngleX_Degree);

			if (_viewport.RenderOptions.SmoothCamera) {
				DeltaLookAt.X += (float)(-distX * Math.Cos(angleX_Rad) - distZ * Math.Sin(angleX_Rad)) / 5f * (float)TranslateMultiplier;
				DeltaLookAt.Z += (float)(distX * Math.Sin(angleX_Rad) - distZ * Math.Cos(angleX_Rad)) / 5f * (float)TranslateMultiplier;
			}
			else {
				LookAt.X += (float)(-distX * Math.Cos(angleX_Rad) - distZ * Math.Sin(angleX_Rad)) * (float)TranslateMultiplier;
				LookAt.Z += (float)(distX * Math.Sin(angleX_Rad) - distZ * Math.Cos(angleX_Rad)) * (float)TranslateMultiplier;
			}

			if (_viewport._request != null && _viewport._request.Gnd != null && _viewport.RenderOptions.ViewStickToGround) {
				var x = LookAt.X / 10f;
				var z = LookAt.Z / 10f;

				var xi = (int)x;
				var yi = (int)z;

				var cube = _viewport._request.Gnd[xi, _viewport._request.Gnd.Height - yi];

				if (cube != null && cube.TileUp != -1) {
					var y2 = -cube[0];
					var dif = (y2 - LookAt.Y) * 0.05 * (_viewport.RenderOptions.UseClientPov ? 0.5 : 1);
					LookAt.Y += (float)dif;
				}
			}

			_viewport.IsRotatingCamera = false;
		}
	}
}
