using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GRF.Image;
using OpenTK.Graphics.OpenGL;
using Utilities;

namespace GRFEditor.WPF.PreviewTabs.GLGroup {
	public static class GLHelper {
		public static Dictionary<string, int> IndexedTextures = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		public static Dictionary<string, GrfImage> IndexedImages = new Dictionary<string, GrfImage>(StringComparer.OrdinalIgnoreCase);

		public static int LoadTexture(string file, string uniqueIdentifier) {
			try {
				if (IndexedTextures.ContainsKey(uniqueIdentifier)) {
					return IndexedTextures[uniqueIdentifier];
				}

				return LoadTexture(new GrfImage(file), uniqueIdentifier);
			}
			catch {
				return -1;
			}
		}

		public static int LoadTexture(GrfImage image, string uniqueIdentifier) {
			try {
				if (IndexedTextures.ContainsKey(uniqueIdentifier)) {
					return IndexedTextures[uniqueIdentifier];
				}

				IndexedImages[uniqueIdentifier] = image.Copy();
				image.Convert(GrfImageType.Bgra32);

				for (int i = 0; i < image.Pixels.Length; i += 4) {
					if (image.Pixels[i + 0] < 10 &&
						image.Pixels[i + 1] < 10 &&
						image.Pixels[i + 2] < 10) {
						image.Pixels[i + 0] = 0;
						image.Pixels[i + 1] = 0;
						image.Pixels[i + 2] = 0;
						image.Pixels[i + 3] = 0;
					}
					else if (image.Pixels[i + 0] >= 252 &&
							 image.Pixels[i + 1] < 10 &&
							 image.Pixels[i + 2] >= 252) {
						image.Pixels[i + 0] = 0;
						image.Pixels[i + 1] = 0;
						image.Pixels[i + 2] = 0;
						image.Pixels[i + 3] = 0;
					}
				}

				int tex;
				GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

				GL.GenTextures(1, out tex);
				GL.BindTexture(TextureTarget.Texture2D, tex);

				GCHandle pinnedArray = GCHandle.Alloc(image.Pixels, GCHandleType.Pinned);
				IntPtr pointer = pinnedArray.AddrOfPinnedObject();

				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0,
					PixelFormat.Bgra, PixelType.UnsignedByte, pointer);

				pinnedArray.Free();

				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

				//GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
				//GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

				IndexedTextures[uniqueIdentifier] = tex;
				return tex;
			}
			catch {
				return -1;
			}
		}

		public static BlendingFactorSrc GetOpenGlBlendFromDirectXSrc(int dxBlend) {
			switch (dxBlend) {
				case 0:	// ??
					return BlendingFactorSrc.Zero;
				case 1:	// D3DBLEND_ZERO
					return BlendingFactorSrc.Zero;
				case 2:	// D3DBLEND_ONE
					return BlendingFactorSrc.One;
				case 3:	// D3DBLEND_SRCCOLOR
					return BlendingFactorSrc.SrcColor;
				case 4:	// D3DBLEND_INVSRCCOLOR
					return BlendingFactorSrc.OneMinusSrcColor;
				case 5:	// D3DBLEND_SRCALPHA
					return BlendingFactorSrc.SrcAlpha;
				case 6:	// D3DBLEND_INVSRCALPHA
					return BlendingFactorSrc.OneMinusSrcAlpha;
				case 7:	// D3DBLEND_DESTALPHA
					return BlendingFactorSrc.DstAlpha;
				case 8:	// D3DBLEND_INVDESTALPHA
					return BlendingFactorSrc.OneMinusDstAlpha;
				case 9:	// D3DBLEND_DESTCOLOR
					return BlendingFactorSrc.DstColor;
				case 10: // D3DBLEND_INVDESTCOLOR
					return BlendingFactorSrc.OneMinusDstColor;
				case 11: // D3DBLEND_SRCALPHASAT
					return BlendingFactorSrc.SrcAlphaSaturate;
				case 12: // D3DBLEND_BOTHSRCALPHA
					return BlendingFactorSrc.Src1Alpha;
				case 13: // D3DBLEND_BOTHINVSRCALPHA
					return BlendingFactorSrc.OneMinusSrc1Alpha;
				default:
					Z.F();
					break;
			}

			return BlendingFactorSrc.SrcAlpha;
		}

		public static BlendingFactorDest GetOpenGlBlendFromDirectXDest(int dxBlend) {
			switch (dxBlend) {
				case 0:	// ??
					return BlendingFactorDest.Zero;
				case 1:	// D3DBLEND_ZERO
					return BlendingFactorDest.Zero;
				case 2:	// D3DBLEND_ONE
					return BlendingFactorDest.One;
				case 3:	// D3DBLEND_SRCCOLOR
					return BlendingFactorDest.SrcColor;
				case 4:	// D3DBLEND_INVSRCCOLOR
					return BlendingFactorDest.OneMinusSrcColor;
				case 5:	// D3DBLEND_SRCALPHA
					return BlendingFactorDest.SrcAlpha;
				case 6:	// D3DBLEND_INVSRCALPHA
					return BlendingFactorDest.OneMinusSrcAlpha;
				case 7:	// D3DBLEND_DESTALPHA
					return BlendingFactorDest.One;
				//return BlendingFactorDest.DstAlpha;
				case 8:	// D3DBLEND_INVDESTALPHA
					return BlendingFactorDest.OneMinusDstAlpha;
				case 9:	// D3DBLEND_DESTCOLOR
					return BlendingFactorDest.DstColor;
				case 10: // D3DBLEND_INVDESTCOLOR
					return BlendingFactorDest.OneMinusDstColor;
				case 11: // D3DBLEND_SRCALPHASAT
					return BlendingFactorDest.SrcAlphaSaturate;
				case 12: // D3DBLEND_BOTHSRCALPHA
					return BlendingFactorDest.Src1Alpha;
				case 13: // D3DBLEND_BOTHINVSRCALPHA
					return BlendingFactorDest.OneMinusSrc1Alpha;
				default:
					Z.F();
					break;
			}

			return BlendingFactorDest.SrcAlpha;
		}

		public static double ToRad(double angle) {
			return angle * (Math.PI / 180f);
		}

		public static float ToRad(float angle) {
			return (float)(angle * (Math.PI / 180f));
		}
	}
}
