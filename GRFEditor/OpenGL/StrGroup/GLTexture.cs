using System;
using System.Runtime.InteropServices;
using GRF.Core;
using GRF.Image;
using OpenTK.Graphics.OpenGL;

namespace GRFEditor.WPF.PreviewTabs.GLGroup {
	public class GLTexture {
		private bool _loaded;
		public int Id { get; set; }
		public string GrfPath { get; set; }
		private GrfImage _image;

		public GLTexture() {
		}

		public GLTexture(GrfImage image) {
			int tex;
			GL.GenTextures(1, out tex);
			Id = tex;
			_image = image;
			GL.BindTexture(TextureTarget.Texture2D, tex);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
			
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
			_loaded = true;
		}

		public void Unload() {
			GL.DeleteTexture(Id);
		}

		public void Load(GrfHolder grf) {
			if (_image == null) {
				var entry = grf.FileTable.TryGet(GrfPath);

				if (entry == null) {
					return;
				}

				_image = new GrfImage(entry);
			}

			_image.Convert(GrfImageType.Bgra32);

			for (int i = 0; i < _image.Pixels.Length; i += 4) {
				if (_image.Pixels[i + 0] < 10 &&
					_image.Pixels[i + 1] < 10 &&
					_image.Pixels[i + 2] < 10) {
					_image.Pixels[i + 0] = 0;
					_image.Pixels[i + 1] = 0;
					_image.Pixels[i + 2] = 0;
					_image.Pixels[i + 3] = 0;
				}
				else if (_image.Pixels[i + 0] >= 252 &&
						 _image.Pixels[i + 1] < 10 &&
						 _image.Pixels[i + 2] >= 252) {
					_image.Pixels[i + 0] = 0;
					_image.Pixels[i + 1] = 0;
					_image.Pixels[i + 2] = 0;
					_image.Pixels[i + 3] = 0;
				}
			}

			GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
			GL.BindTexture(TextureTarget.Texture2D, Id);

			GCHandle pinnedArray = GCHandle.Alloc(_image.Pixels, GCHandleType.Pinned);
			IntPtr pointer = pinnedArray.AddrOfPinnedObject();

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, _image.Width, _image.Height, 0,
				PixelFormat.Bgra, PixelType.UnsignedByte, pointer);

			pinnedArray.Free();

			GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.MirroredRepeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.MirroredRepeat);

			_loaded = true;
		}

		public void SetWrapMode(int mode) {
			Bind();
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, mode);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, mode);
		}

		public void SetSubImage(byte[] data, int x, int y, int width, int height) {
			Bind();
			GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
			IntPtr pointer = pinnedArray.AddrOfPinnedObject();

			GL.TexSubImage2D(TextureTarget.Texture2D, 0, x, y, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pointer);
			pinnedArray.Free();
		}

		public void Resize(int width, int height) {
			Bind();
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
		}

		public void Bind() {
			if (_loaded) {
				GL.BindTexture(TextureTarget.Texture2D, Id);
			}
		}
	}
}
