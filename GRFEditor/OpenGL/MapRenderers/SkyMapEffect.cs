using GRFEditor.OpenGL.MapComponents;
using OpenTK;
using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using GRF.FileFormats.RswFormat;

namespace GRFEditor.OpenGL.MapRenderers {
	public class SkymapSettings {
		public Vector4 Bg_Color = new Vector4(102, 152, 204, 255) / 255f;
		public bool Star_Effect;
		public bool BG_Fog;
		public List<SkyEffect> SkyEffects = new List<SkyEffect>();

		public bool IsModified { get; set; }
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct ParticleParams {
		public float Size;
		public float Size_Extra;
		public float Height;
		public float Height_Extra;
		public float Alpha_Inc_Time;
		public float Alpha_Inc_Time_Extra;
		public float Alpha_Inc_Speed;
		public float Alpha_Dec_Time;
		public float Alpha_Dec_Time_Extra;
		public float Alpha_Dec_Speed;
		public float Expand_Rate;
		public float UVSplit;
		public float UVCycleSpeed;
		public float ScaleX;
		public float ScaleY;
		public int DirMode;
		public Vector3 ForcedDir;
	}

	public sealed class SkyMapEffectTemplates {
		public static ParticleParams GetDefaultShaderParameters() {
			ParticleParams parameters = new ParticleParams();
			parameters.ScaleX = 1;
			parameters.ScaleY = 1;
			return parameters;
		}

		public static SkyEffect GetStarTemplate() {
			var effect = new SkyEffect();
			// This Num setting is a bit useless because the stars are created around the player
			// This is just an approximation
			effect.Num = -1;
			effect.NumPerSquared = (float)(60f * 4 / Math.Pow(400, 2));
			effect.Color = new Vector3(1);
			effect.SrcMode = BlendingFactor.SrcAlpha;
			effect.DstMode = BlendingFactor.DstAlpha;
			effect.ShaderParameters = GetDefaultShaderParameters();
			effect.ShaderParameters.Size = 20f;
			effect.ShaderParameters.Size_Extra = 10f;
			effect.ShaderParameters.Expand_Rate = 0f;
			effect.ShaderParameters.Alpha_Inc_Time = 80f;
			effect.ShaderParameters.Alpha_Inc_Time_Extra = 0f;
			effect.ShaderParameters.Alpha_Inc_Speed = 3.3f;
			effect.ShaderParameters.Alpha_Dec_Time = 500f;
			effect.ShaderParameters.Alpha_Dec_Time_Extra = 300f;
			effect.ShaderParameters.Alpha_Dec_Speed = 0.5f;
			effect.ShaderParameters.Height = 80f;
			effect.ShaderParameters.Height_Extra = 10f;
			effect.ShaderParameters.UVCycleSpeed = 0.143f;
			effect.TextureType = CloudEffectTextureType.Star;
			effect.IsStarEffect = true;
			return effect;
		}

		public static SkyEffect GetTemplate(int type, int subtype = 0) {
			SkyEffect effect = new SkyEffect();
			effect.OldCloudEffect = type;

			// Default values, very common
			effect.Color = new Vector3(1);
			effect.SrcMode = BlendingFactor.SrcAlpha;
			effect.DstMode = BlendingFactor.OneMinusSrcAlpha;
			effect.ShaderParameters.Size = 20f;
			effect.ShaderParameters.Size_Extra = 20f;
			effect.ShaderParameters.Expand_Rate = 0.05f;
			effect.ShaderParameters.Alpha_Inc_Time = 80f;
			effect.ShaderParameters.Alpha_Inc_Time_Extra = 200f;
			effect.ShaderParameters.Alpha_Inc_Speed = 1f;
			effect.ShaderParameters.Alpha_Dec_Time = 300f;
			effect.ShaderParameters.Alpha_Dec_Time_Extra = 200f;
			effect.ShaderParameters.Alpha_Dec_Speed = 0.5f;
			effect.ShaderParameters.UVCycleSpeed = 0f;

			switch (type) {
				case -1:
					effect.Num = -1;
					effect.NumPerSquared = (float)(60f * 4 / Math.Pow(400, 2));
					effect.TextureType = CloudEffectTextureType.Cloud;
					effect.ShaderParameters.Height = 40f;
					effect.ShaderParameters.Height_Extra = 10f;
					effect.OldCloudEffect = 0;
					break;
				case 1:
					// Decompiler: 400 range, 60 clouds per quadrants
					// 40 cells
					effect.Num = -1;
					effect.NumPerSquared = (float)(60f * 4 / Math.Pow(400, 2));
					effect.TextureType = CloudEffectTextureType.Cloud;
					effect.ShaderParameters.ScaleY = -1;
					effect.ShaderParameters.Height = 40f;
					effect.ShaderParameters.Height_Extra = 10f;
					break;
				case 2:
					// Decompiler: 300 range, 40 clouds per quadrants
					// 30 cells
					effect.Num = -1;
					effect.NumPerSquared = (float)(40f * 4 / Math.Pow(300, 2));
					effect.TextureType = CloudEffectTextureType.Cloud;
					effect.ShaderParameters.ScaleX = -1;
					effect.ShaderParameters.ScaleY = -1;
					effect.ShaderParameters.Height = 0f;
					effect.ShaderParameters.Height_Extra = 10f;
					break;
				case 3:
					// Decompiler: 300 range, 80 clouds per quadrants
					// 30 cells
					effect.Num = -1;
					effect.NumPerSquared = (float)(80f * 4 / Math.Pow(300, 2));
					effect.Color = new Vector3(196 / 255f, 133 / 255f, 111 / 255f);
					effect.TextureType = CloudEffectTextureType.Fog;
					effect.ShaderParameters.ScaleY = -1;
					effect.ShaderParameters.Height = 0f;
					effect.ShaderParameters.Height_Extra = -10f;
					effect.ShaderParameters.DirMode = 1;	// No movement
					effect.AboveGroundOnly = true;
					effect.ShaderParameters.Height = 99999f;
					break;
				case 4:
					// Decompiler: 400 range, 80 clouds per quadrants
					// 40 cells
					effect.Num = -1;
					effect.NumPerSquared = (float)(80f * 4 / Math.Pow(400, 2));
					effect.TextureType = CloudEffectTextureType.Cloud;
					effect.ShaderParameters.ScaleX = -1;
					effect.ShaderParameters.ScaleY = -1;
					effect.ShaderParameters.Height = 40f;
					effect.ShaderParameters.Height_Extra = 10f;
					effect.ShaderParameters.DirMode = 2; // Right-sided movement
					effect.ShaderParameters.ForcedDir = new Vector3(10, 0, 0);
					break;
				case 5:
					// Decompiler: 300 range, 80 clouds per quadrants
					// 30 cells
					effect.Num = -1;
					effect.NumPerSquared = (float)(80f * 4 / Math.Pow(300, 2));
					effect.Color = new Vector3(94 / 255f, 0, 0);
					effect.TextureType = CloudEffectTextureType.Cloud;
					effect.ShaderParameters.ScaleY = -1;
					effect.ShaderParameters.Height = 20f;
					effect.ShaderParameters.Height_Extra = 10f;
					break;
				case 7:
					// Decompiler: 400 range, 80 clouds per quadrants
					// 40 cells
					effect.Num = -1;
					effect.NumPerSquared = (float)(80f * 4 / Math.Pow(400, 2));
					effect.Color = new Vector3(0, 0, 0);
					effect.TextureType = CloudEffectTextureType.Cloud;
					effect.ShaderParameters.ScaleY = -1;
					effect.ShaderParameters.Height = 40f;
					effect.ShaderParameters.Height_Extra = 10f;
					break;
				case 8:
					// Decompiler: 400 range, 80 clouds per quadrants
					// 40 cells
					effect.Num = -1;
					effect.NumPerSquared = (float)(80f * 4 / Math.Pow(400, 2));
					effect.Color = new Vector3(255 / 255f, 180 / 255f, 180 / 255f);
					effect.TextureType = CloudEffectTextureType.Cloud;
					effect.ShaderParameters.ScaleY = -1;
					effect.ShaderParameters.Height = 40f;
					effect.ShaderParameters.Height_Extra = 10f;
					break;
				case 9:
					// Decompiler: 400 range, 65 clouds per quadrants
					// 40 cells
					effect.Num = -1;
					effect.NumPerSquared = (float)(65f * 4 / Math.Pow(400, 2));
					effect.ShaderParameters.Alpha_Inc_Time = 100f;
					effect.ShaderParameters.Alpha_Inc_Time_Extra = 0;
					effect.ShaderParameters.Alpha_Inc_Speed = 2.45f;
					effect.TextureType = CloudEffectTextureType.Star;
					effect.SrcMode = BlendingFactor.SrcAlpha;
					effect.DstMode = BlendingFactor.One;
					effect.ShaderParameters.Height = 85f;
					effect.ShaderParameters.Height_Extra = 10f;
					effect.ShaderParameters.UVCycleSpeed = 0.143f;
					break;
				case 10:
					// ??
					// 30 cells
					effect.Num = -1;
					effect.NumPerSquared = (float)(60f * 4 / Math.Pow(400, 2));
					effect.ShaderParameters.Alpha_Inc_Time = 100f;
					effect.ShaderParameters.Alpha_Inc_Time_Extra = 0;
					effect.ShaderParameters.Alpha_Inc_Speed = 1.5f;
					effect.Color = new Vector3(94 / 255f, 0, 0);
					effect.TextureType = CloudEffectTextureType.Cloud;
					effect.ShaderParameters.ScaleY = -1;
					effect.ShaderParameters.Height = 30f;
					effect.ShaderParameters.Height_Extra = 10f;
					break;
				case 11:
					// ??
					// 40 cells
					// It should be 40 per quadrant
					// but... 40 seems more realistic because we're not sorting the particles
					// so too many particles will mess up the blending badly
					effect.Num = -1;
					effect.NumPerSquared = (float)(30f * 4 / Math.Pow(400, 2));

					switch (subtype) {
						case 0:
							effect.Color = new Vector3(0 / 255f, 0 / 255f, 0 / 255f);
							effect.SrcMode = BlendingFactor.SrcAlpha;
							effect.DstMode = BlendingFactor.OneMinusSrcAlpha;
							break;
						case 1:
							effect.Color = new Vector3(255 / 255f, 181 / 255f, 181 / 255f);
							effect.SrcMode = BlendingFactor.SrcAlpha;
							effect.DstMode = BlendingFactor.OneMinusSrcAlpha;
							break;
						case 2:
							effect.Color = new Vector3(92 / 255f, 0 / 255f, 0 / 255f);
							effect.SrcMode = BlendingFactor.SrcAlpha;
							effect.DstMode = BlendingFactor.OneMinusSrcAlpha;
							break;
						case 3:
							effect.Color = new Vector3(64 / 255f, 70 / 255f, 203 / 255f);
							effect.SrcMode = BlendingFactor.SrcAlpha;
							effect.DstMode = BlendingFactor.One;
							break;
					}

					effect.ShaderParameters.Alpha_Inc_Time = 100f;
					effect.ShaderParameters.Alpha_Inc_Time_Extra = 0;
					effect.ShaderParameters.Alpha_Inc_Speed = 0.69f;
					effect.TextureType = CloudEffectTextureType.Cloud;
					effect.ShaderParameters.ScaleY = -1;
					effect.ShaderParameters.Height = 30f;
					effect.ShaderParameters.Height_Extra = 20f;
					break;
				case 15:
					// Decompiler: 400 range, 65 clouds per quadrants
					// 40 cells
					effect.Num = -1;
					effect.NumPerSquared = (float)(65f * 4 / Math.Pow(400, 2));
					effect.ShaderParameters.Alpha_Inc_Time = 100f;
					effect.ShaderParameters.Alpha_Inc_Time_Extra = 0;
					effect.ShaderParameters.Alpha_Inc_Speed = 2.45f;
					effect.TextureType = CloudEffectTextureType.Star;
					effect.SrcMode = BlendingFactor.SrcAlpha;
					effect.DstMode = BlendingFactor.One;
					effect.ShaderParameters.Height = -40;
					effect.ShaderParameters.Height_Extra = 10f;
					effect.ShaderParameters.UVCycleSpeed = 0f;
					break;
				default:
					return null;
			}

			return effect;
		}
	}

	public enum CloudEffectTextureType {
		Cloud,
		Star,
		Fog,
	}
	
	public class SkyEffect {
		public ParticleParams ShaderParameters;
		public int Num = 1000;
		public float NumPerSquared = 0;
		public int CullDist;
		public int SplitCount;
		public Vector3 Color = new Vector3(1f);
		public bool AboveGroundOnly = false;
		public int OldCloudEffect { get; set; }
		public bool IsUnloaded { get; set; }
		public bool IsLoaded { get; set; }
		public bool IsEnabled { get; set; }
		public bool IsModified { get; set; }
		public bool IsDeleted { get; set; }
		public bool IsStarEffect { get; set; }

		public List<SkyEffect> SubEffects = new List<SkyEffect>();

		public BlendingFactor SrcMode = BlendingFactor.SrcAlpha;
		public BlendingFactor DstMode = BlendingFactor.OneMinusSrcAlpha;
		public CloudEffectTextureType TextureType = CloudEffectTextureType.Cloud;

		public Texture Atlas;
		public RenderInfo RenderInfo = new RenderInfo();
		public ParticleInstance[] Particles;

		public SkyEffect() {
			ShaderParameters = SkyMapEffectTemplates.GetDefaultShaderParameters();
			IsEnabled = true;
		}

		public void ImportOld(int cloudType) {
			if (cloudType == 11) {
				while (SubEffects.Count != 4) {
					SubEffects.Add(new SkyEffect());
				}

				for (int i = 0; i < 4; i++) {
					SubEffects[i]._copySettings(SkyMapEffectTemplates.GetTemplate(cloudType, i));
				}

				OldCloudEffect = cloudType;
			}
			else {
				var template = SkyMapEffectTemplates.GetTemplate(cloudType);

				if (template == null) {
					IsEnabled = false;
					return;
				}

				_copySettings(template);
			}
		}

		protected void _copySettings(SkyEffect template) {
			Num = template.Num;
			NumPerSquared = template.NumPerSquared;
			Color = template.Color;
			AboveGroundOnly = template.AboveGroundOnly;
			OldCloudEffect = template.OldCloudEffect;
			IsStarEffect = template.IsStarEffect;
			SrcMode = template.SrcMode;
			DstMode = template.DstMode;
			TextureType = template.TextureType;
			SplitCount = template.SplitCount;

			ShaderParameters.Size = template.ShaderParameters.Size;
			ShaderParameters.Size_Extra = template.ShaderParameters.Size_Extra;
			ShaderParameters.Height = template.ShaderParameters.Height;
			ShaderParameters.Height_Extra = template.ShaderParameters.Height_Extra;
			ShaderParameters.Alpha_Inc_Time = template.ShaderParameters.Alpha_Inc_Time;
			ShaderParameters.Alpha_Inc_Time_Extra = template.ShaderParameters.Alpha_Inc_Time_Extra;
			ShaderParameters.Alpha_Inc_Speed = template.ShaderParameters.Alpha_Inc_Speed;
			ShaderParameters.Alpha_Dec_Time = template.ShaderParameters.Alpha_Dec_Time;
			ShaderParameters.Alpha_Dec_Time_Extra = template.ShaderParameters.Alpha_Dec_Time_Extra;
			ShaderParameters.Alpha_Dec_Speed = template.ShaderParameters.Alpha_Dec_Speed;
			ShaderParameters.Expand_Rate = template.ShaderParameters.Expand_Rate;
			ShaderParameters.UVSplit = template.ShaderParameters.UVSplit;
			ShaderParameters.UVCycleSpeed = template.ShaderParameters.UVCycleSpeed;
			ShaderParameters.ScaleX = template.ShaderParameters.ScaleX;
			ShaderParameters.ScaleY = template.ShaderParameters.ScaleY;
			ShaderParameters.DirMode = template.ShaderParameters.DirMode;
			ShaderParameters.ForcedDir = template.ShaderParameters.ForcedDir;

			IsModified = true;
			IsEnabled = true;
			IsUnloaded = false;

			foreach (var subEffect in SubEffects) {
				subEffect.IsEnabled = false;
			}
		}
	}
}
