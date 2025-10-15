using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GRF.FileFormats.TgaFormat;
using GRF.Image;
using GRF.Image.Decoders;
using GRF.Threading;
using GrfToWpfBridge;
using OpenTK.Graphics.OpenGL;
using Utilities;
using Utilities.Extension;

namespace GRFEditor.OpenGL.MapComponents {
	public class TextureLoadRequest {
		public string Resource;
		public Texture Texture;
		public TextureRenderMode RenderMode;
		public RendererLoadRequest Request;
	}

	public class TextureLoaderThread : PausableThread {
		private readonly object _lock = new object();
		private readonly List<TextureLoadRequest> _textures = new List<TextureLoadRequest>();
		public bool IsEnabled = true;
		public bool IsFinished = true;

		public Texture LoadTextureLazy(string texture, string path, TextureRenderMode renderMode, RendererLoadRequest request) {
			lock (_lock) {
				var ttexture = TextureManager.LoadNullTexture(texture, path, request);
				_textures.Add(new TextureLoadRequest { Resource = path, Texture = ttexture, RenderMode = renderMode, Request = request });
				IsPaused = false;
				return ttexture;
			}
		}

		public void Terminate() {
			IsEnabled = false;
			IsPaused = false;
		}

		public void Start() {
			GrfThread.Start(_start, "GRF - TextureLoaderThread thread starter");
		}

		private void _start() {
			while (true) {
				if (!IsEnabled)
					break;

				var textures = new List<TextureLoadRequest>();

				lock (_lock) {
					textures.AddRange(_textures);
					_textures.Clear();
				}

				if (textures.Count == 0) {
					IsFinished = true;
					Pause();
					IsFinished = false;
				}

				foreach (var entry in textures) {
					try {
						if (entry.Request.CancelRequired())
							continue;

						var data = ResourceManager.GetData(entry.Resource);
						GrfImage image = null;

						if (entry.Resource == "backside.bmp") {
							image = new GrfImage(new byte[] { 0, 0, 0, 255 }, 1, 1, GrfImageType.Bgra32);
						}
						else if (data != null) {
							try {
								image = new GrfImage(data);
							}
							catch {
							}
						}

						if (entry.Request.CancelRequired())
							continue;

						if (image == null) {
							image = new GrfImage(new byte[] { 0, 0, 255, 255 }, 1, 1, GrfImageType.Bgra32);
						}

						entry.Texture.Set(image, entry.RenderMode);
					}
					catch {
						GLHelper.OnLog(() => "Failed: \"" + entry + "\", Message: thrown exception.");
					}
				}
			}
		}
	}

	public enum TextureRenderMode {
		RsmTexture,
		GndTexture,
		WaterTexture,
		ShadowMapTexture,
		CloudTexture,
	};

	public static class TextureManager {
		/// <summary>
		/// The stored textures.
		/// </summary>
		private static readonly Dictionary<string, Utilities.Extension.Tuple<Texture, int>> _bufferedTextures = new Dictionary<string, Utilities.Extension.Tuple<Texture, int>>(StringComparer.OrdinalIgnoreCase);
		
		/// <summary>
		/// The texture counter for each context.
		/// </summary>
		private static readonly Dictionary<object, Dictionary<string, int>> _contextBufferedTextures = new Dictionary<object, Dictionary<string, int>>();
		public static object TextureManagerLock = new object();
		private static readonly List<TextureLoaderThread> _threads = new List<TextureLoaderThread>();
		private static int _nextThread;
		public static Texture DefaultTexture;

		static TextureManager() {
			_threads.Add(new TextureLoaderThread());
			_threads.Add(new TextureLoaderThread());
			_threads.Add(new TextureLoaderThread());
			_threads.Add(new TextureLoaderThread());
			_threads.Add(new TextureLoaderThread());
			_threads.Add(new TextureLoaderThread());
			_threads.Add(new TextureLoaderThread());
			_threads.Add(new TextureLoaderThread());

			_threads.ForEach(p => p.Start());
		}

		public static Dictionary<string, Utilities.Extension.Tuple<Texture, int>> BufferedTextures {
			get { return _bufferedTextures; }
		}

		public static Dictionary<object, Dictionary<string, int>> ContextBufferedTextures {
			get { return _contextBufferedTextures; }
		}

		public static void ExitTextureThreads() {
			_threads.ForEach(p => p.Terminate());
		}

		private static void _addContextTexture(object context, string texture) {
			Dictionary<string, int> contextTextureCounter;

			if (!_contextBufferedTextures.TryGetValue(context, out contextTextureCounter)) {
				contextTextureCounter = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
				_contextBufferedTextures[context] = contextTextureCounter;
			}

			if (contextTextureCounter.ContainsKey(texture))
				contextTextureCounter[texture]++;
			else
				contextTextureCounter[texture] = 1;
		}

		public static Texture LoadTextureAsync(string texture, string path, TextureRenderMode renderMode, RendererLoadRequest request) {
			if (_bufferedTextures.ContainsKey(texture)) {
				_addContextTexture(request.Context, texture);
				_bufferedTextures[texture].Item2++;
				return _bufferedTextures[texture].Item1;
			}

			var ttexture = _threads[_nextThread].LoadTextureLazy(texture, path, renderMode, request);
			_nextThread++;
			_nextThread = _nextThread % _threads.Count;
			return ttexture;
		}

		public static Texture LoadTexture(string texture, string path, object context) {
			_addContextTexture(context, texture);

			if (_bufferedTextures.ContainsKey(texture)) {
				_bufferedTextures[texture].Item2++;
				return _bufferedTextures[texture].Item1;
			}

			var tuple = new Utilities.Extension.Tuple<Texture, int>(new Texture(texture, new GrfImage(ResourceManager.GetData(path))), 1);
			_bufferedTextures[texture] = tuple;
			return tuple.Item1;
		}

		public static Texture LoadTexture(string texture, byte[] data, object context) {
			_addContextTexture(context, texture);

			if (_bufferedTextures.ContainsKey(texture)) {
				_bufferedTextures[texture].Item2++;
				return _bufferedTextures[texture].Item1;
			}

			var tuple = new Utilities.Extension.Tuple<Texture, int>(new Texture(texture, new GrfImage(data)), 1);
			_bufferedTextures[texture] = tuple;
			return tuple.Item1;
		}

		public static Texture LoadNullTexture(string texture, string path, RendererLoadRequest request) {
			_addContextTexture(request.Context, texture);

			if (_bufferedTextures.ContainsKey(texture)) {
				_bufferedTextures[texture].Item2++;
				return _bufferedTextures[texture].Item1;
			}

			var ttexture = new Texture(texture);
			_bufferedTextures[texture] = new Utilities.Extension.Tuple<Texture, int>(ttexture, 1);
			return ttexture;
		}

		public static void UnloadTexture(string texture, object context) {
			Dictionary<string, int> contextTextureCounter;

			if (!_contextBufferedTextures.TryGetValue(context, out contextTextureCounter)) {
				GLHelper.OnLog(() => "Error: " + "This context doesn't exist while trying to unload a texture: " + context + ", texture: " + texture);
				return;
			}

			if (contextTextureCounter.ContainsKey(texture)) {
				contextTextureCounter[texture]--;

				if (contextTextureCounter[texture] == 0)
					contextTextureCounter.Remove(texture);
			}
			else {
				GLHelper.OnLog(() => "Error: " + "This texture does not exist in this context: " + context + ", texture: " + texture);
				return;
			}

			if (_bufferedTextures.ContainsKey(texture)) {
				_bufferedTextures[texture].Item2--;

				if (_bufferedTextures[texture].Item2 == 0) {
					_bufferedTextures[texture].Item1.Unload();
					_bufferedTextures.Remove(texture);
				}
			}
		}

		public static void UnloadAllTextures(object context) {
			Dictionary<string, int> contextTextureCounter;

			if (!_contextBufferedTextures.TryGetValue(context, out contextTextureCounter)) {
				return;
			}

			List<string> keys = new List<string>();

			foreach (var contextTexture_c in contextTextureCounter) {
				var contextTexture = contextTexture_c;
				var t = _bufferedTextures[contextTexture.Key];
				t.Item2 -= contextTexture.Value;

				if (t.Item2 < 0) {
					GLHelper.OnLog(() => "Error: " + "Attempted to unload a texture more often than it has instances of: " + contextTexture.Key + ", context: " + context.GetHashCode());
				}
				else if (t.Item2 == 0) {
					t.Item1.Unload();
					keys.Add(contextTexture.Key);
				}
			}

			_contextBufferedTextures.Remove(context);

			foreach (var key in keys) {
				_bufferedTextures.Remove(key);
			}
		}
	}

	public class Texture {
		private int _id;

		public string Resource { get; private set; }
		public bool IsSemiTransparent { get; private set; }
		public bool IsLoaded { get; private set; }
		public GrfImage Image { get; private set; }
		public bool IsUnloaded { get; set; }
		public bool Permanent { get; set; }
		public bool FixTransparency { get; set; }
		public TextureRenderMode RenderMode = TextureRenderMode.RsmTexture;
		public static bool EnableMipmap { get; set; }
		public bool IsDithered { get; set; }
		public bool TransparencyFixed { get; set; }

		static Texture() {
			EnableMipmap = false;
		}

		public Texture(string resource) {
			Resource = resource;
			IsSemiTransparent = Resource.IsExtension(".tga", ".png");
			FixTransparency = true;
		}

		public Texture(string resource, GrfImage image, bool load = true, TextureRenderMode renderMode = TextureRenderMode.RsmTexture) {
			IsLoaded = false;
			Resource = resource;
			Image = image;
			IsSemiTransparent = Resource.IsExtension(".tga", ".png");
			RenderMode = renderMode;
			FixTransparency = true;

			if (load) {
				Reload();
				IsLoaded = true;
			}
		}

		public void Set(GrfImage image, TextureRenderMode renderMode = TextureRenderMode.RsmTexture) {
			if (IsUnloaded)
				return;

			RenderMode = renderMode;
			IsLoaded = false;

			_setFormatToBgra32(image);
			_imageDitherAndPinkRemoval(image);

			// Cannot set Image early, otherwise the OpenGL viewport will try to load the image before it's processed
			Image = image;
		}

		private void _setFormatToBgra32(GrfImage image) {
			if (!TransparencyFixed && !String.IsNullOrEmpty(Resource) && FixTransparency) {
				if (image.GrfImageType != GrfImageType.Bgra32) {
					image.Convert(new Bgra32FormatConverter());
				}
			}

			TransparencyFixed = true;
		}

		public void Unload() {
			if (_id > 0) {
				IsUnloaded = true;
				Image = null;
				TransparencyFixed = false;

				GL.DeleteTexture(_id);
				OpenGLMemoryManager.DelTextureId(_id);
				GLHelper.OnLog(() => "Unloaded: \"" + Resource + "\", Message: texID " + _id + ".");
			}
		}
		
		public int Id {
			get {
				if (IsLoaded) {
					return _id;
				}

				return 0;
			}
		}

		public int Size { get; set; }

		public void Reload() {
			if (IsUnloaded)
				return;

			try {
				_setFormatToBgra32(Image);
				_imageDitherAndPinkRemoval(Image);

				GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
				_id = GL.GenTexture();
				OpenGLMemoryManager.AddTextureId(_id);
				GL.BindTexture(TextureTarget.Texture2D, _id);

				if (IsSemiTransparent) {
					if (Resource.IsExtension(".tga")) {
						Image.Flip(FlipDirection.Vertical);
					}
				}

				GCHandle pinnedArray = GCHandle.Alloc(Image.Pixels, GCHandleType.Pinned);
				IntPtr pointer = pinnedArray.AddrOfPinnedObject();

				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Image.Width, Image.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, pointer);

				_setTextureMode();

				pinnedArray.Free();
				IsLoaded = true;
				Size = Image.Pixels.Length;
				Image.Close();
				Image = null;

				GLHelper.OnLog(() => "Loaded: \"" + Resource + "\", Message: texID " + Id + ".");
			}
			catch {
				GLHelper.OnLog(() => "Failed: \"" + Resource + "\", Message: Reload() has thrown an exception.");
			}
		}

		private void _imageDitherAndPinkRemoval(GrfImage image) {
			if (IsDithered)
				return;

			// RO textures use lower resolution, but we're emulating that process instead
			//bool dithering = RenderMode == TextureRenderMode.RsmTexture && RsmEditorConfiguration.UseIngameColorDepth;
			int ditherDivider = 8;
			int ditherDividerShift = 3;
			float ditherMultiplier = 8.25f;
			
			if (Resource.IsExtension(".png", ".tga")) {
				ditherDividerShift = 4;
				ditherDivider = 16;
				ditherMultiplier = 17;
			}

			byte rT = (byte)(Math.Ceiling((ditherDivider / ditherMultiplier) * 255) - 1);
			byte gT = (byte)(255 - rT);
			byte bT = rT;

			//image.ChangePinkToBlack(rT, gT, bT);
			image.DitherAndChangePinkToBlack(rT, gT, bT, ditherDividerShift, ditherMultiplier);
			IsDithered = true;
		}

		private void _setTextureMode() {
			switch (RenderMode) {
				case TextureRenderMode.CloudTexture:
					GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapNearest);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.LinearMipmapNearest);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.MirroredRepeat);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.MirroredRepeat);
					break;
				case TextureRenderMode.ShadowMapTexture:
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
					break;
				case TextureRenderMode.RsmTexture:
					if (EnableMipmap) {
						GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
					}
					else {
						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
					}

					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
					break;
				default:
					if (EnableMipmap) {
						GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
					}
					else {
						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
					}

					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.MirroredRepeat);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.MirroredRepeat);
					break;
				case TextureRenderMode.GndTexture:
					if (EnableMipmap) {
						GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
					}
					else {
						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
					}

					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
					break;
			}
		}

		public void ReloadParameter() {
			Bind();
			_setTextureMode();
			GLHelper.OnLog(() => "Texture: \"" + Resource + "\", Message: texID " + Id + ", reloaded parameter.");
		}

		public void Bind() {
			if (!IsLoaded) {
				if (Image != null) {
					Reload();
				}
				else {
					if (TextureManager.DefaultTexture == null) {
						TextureManager.DefaultTexture = new Texture("DEFAULT", new GrfImage(new byte[] { 0, 0, 255, 255 }, 1, 1, GrfImageType.Bgra32));
					}

					TextureManager.DefaultTexture.Bind();
					return;
				}
			}

			GL.BindTexture(TextureTarget.Texture2D, _id);
		}

		public override int GetHashCode() {
			return Resource.GetHashCode();
		}
	}
}
