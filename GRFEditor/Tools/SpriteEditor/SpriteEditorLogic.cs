using ErrorManager;
using GRF.FileFormats.ActFormat;
using GRF.FileFormats.SprFormat;
using GRF.Image;
using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;

namespace GRFEditor.Tools.SpriteEditor {
	public class SpriteEditorLogic {
		private Spr _spr;
		private Act _act;

		public SpriteEditorLogic(Spr spr, Act act) {
			_spr = spr;
			_act = act;
		}

		public void Delete(GrfImageType type, List<int> list) {
			_execute(() => {
				foreach (var id in list.OrderByDescending(p => p)) {
					_act.Commands.SpriteRemove(id);
				}
			});
		}

		public void Flip(GrfImageType type, List<int> list, FlipDirection direction) {
			_execute(() => {
				foreach (var id in list) {
					_act.Commands.SpriteFlip(id, direction);
				}
			});
		}

		public void InsertBefore(GrfImageType type, int insertIndex, List<string> files) {
			if (files.Count == 0)
				return;

			_execute(() => {
				var filesCopy = files.ToList();
				filesCopy.Reverse();

				foreach (var file in filesCopy) {
					GrfImage image = new GrfImage(file);
					image = _ensureCorrectType(image, type);
					_act.Commands.SpriteInsert(insertIndex, type, image);
				}
			});
		}

		public void InsertAfter(GrfImageType type, int insertIndex, List<string> files) {
			if (files == null || files.Count == 0)
				return;

			_execute(() => {
				int offset = 1;

				foreach (var file in files) {
					GrfImage image = new GrfImage(file);
					image = _ensureCorrectType(image, type);
					_act.Commands.SpriteInsert(insertIndex + offset++, type, image);
				}
			});
		}

		public void Replace(GrfImageType type, int insertIndex, List<string> files) {
			if (files.Count == 0)
				return;

			_execute(() => {
				int index = insertIndex;

				foreach (var file in files) {
					if (index >= _getContainerLength(type))
						break;

					GrfImage image = new GrfImage(file);
					image = _ensureCorrectType(image, type);

					_act.Commands.SpriteReplaceAt(new SpriteIndex(index, type), image);
					index++;
				}
			});
		}

		public void Convert(GrfImageType type, List<int> list) {
			if (list.Count == 0)
				return;

			try {
				_act.Commands.ActEditBegin("");
				
				if (type == GrfImageType.Bgra32 && (_spr.Palette == null || _spr.Palette.BytePalette == null)) {
					byte[] palette = new byte[1024];
					palette[0] = 255;
					palette[1] = 0;
					palette[2] = 255;
					_act.Sprite.SetPalette(palette);
				}

				GrfImageType targetType = type == GrfImageType.Indexed8 ? GrfImageType.Bgra32 : GrfImageType.Indexed8;

				List<GrfImage> toAdd = new List<GrfImage>();

				foreach (var id in list) {
					GrfImage image = _spr.GetImage(id);
					image = _ensureCorrectType(image, targetType);
					
					if (image.GrfImageType == GrfImageType.Indexed8) {
						if (!Methods.ByteArrayCompare(image.Palette, _act.Sprite.Palette.BytePalette)) {
							_act.Sprite.SetPalette(image.Palette);
						}
					}

					toAdd.Add(image);
				}

				foreach (var id in list.OrderByDescending(p => p)) {
					_act.Sprite.Remove(id);
				}

				foreach (var image in toAdd) {
					_act.Sprite.InsertAny(image);
				}
			}
			catch (Exception err) {
				_act.Commands.ActCancelEdit();
				ErrorHandler.HandleException(err);
			}
			finally {
				_act.Commands.ActEditEnd();
			}
		}

		internal void ChangeId(GrfImageType type, int currentId, int newId) {
			if (currentId < 0 || newId < 0)
				return;

			if (type == GrfImageType.Indexed8) {
				newId = Methods.Clamp(newId, 0, _getContainerLength(type) - 1);
			}
			else {
				newId = Methods.Clamp(newId, _spr.NumberOfIndexed8Images, _spr.NumberOfImagesLoaded - 1);
			}

			if (currentId == newId)
				return;

			try {
				_act.Commands.ActEditBegin("");
				GrfImage image = _spr.GetImage(currentId);

				if (newId < currentId) {
					_act.Sprite.Images.Insert(newId, image);
					_act.Sprite.Images.RemoveAt(currentId + 1);
				}
				else {
					_act.Sprite.Images.Insert(newId + 1, image);
					_act.Sprite.Images.RemoveAt(currentId);
				}
			}
			catch (Exception err) {
				_act.Commands.ActCancelEdit();
				ErrorHandler.HandleException(err);
			}
			finally {
				_act.Commands.ActEditEnd();
			}
		}

		private int _getContainerLength(GrfImageType type) {
			return type == GrfImageType.Indexed8 ? _spr.NumberOfIndexed8Images : _spr.NumberOfBgra32Images;
		}

		private GrfImage _ensureCorrectType(GrfImage image, GrfImageType targetType) {
			if (targetType == GrfImageType.Indexed8) {
				image = GrfImage.SprConvert(_spr, image, false, GrfImage.SprTransparencyMode.PixelIndexZero, GrfImage.SprConvertMode.MergeRgb);
			}
			else {
				if (image.GrfImageType == GrfImageType.Indexed8) {
					image.MakeFirstPixelTransparent();
				}

				image.Convert(GrfImageType.Bgra32);
			}

			return image;
		}

		private void _execute(System.Action action) {
			try {
				_act.Commands.BeginNoDelay();
				action();
			}
			catch (Exception err) {
				_act.Commands.CancelEdit();
				ErrorHandler.HandleException(err);
			}
			finally {
				_act.Commands.End();
			}
		}
	}
}
