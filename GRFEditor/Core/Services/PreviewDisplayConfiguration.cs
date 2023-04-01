using System;
using System.Collections.Generic;
using System.Reflection;

namespace GRFEditor.Core.Services {
	public class PreviewDisplayConfiguration {
		public PreviewDisplayConfiguration() {
			ShowComboBox = false;
			ShowAnimation = false;
			ShowFullImageButton = false;
			ClearAnimationImages = true;
			ShowTextEditor = false;
			ShowRawStructureTextEditor = false;
			ResetImagePreview = true;
			ShowImagePreview = false;
			CenterImagePreview = false;
			ShowMapExtractor = false;
			ShowGat = false;
			ShowDb = false;
			ShowEditSprite = false;
			ShowFolderPreview = false;
			ShowFolderStructurePreview = false;
			ShowGrfProperties = true;
			//ShowGatEditor = false;
			ShowRsm = false;
			ShowSoundPreview = false;
			ShowLubDecompiler = false;
			ShowContainerPreview = false;
			ShowSpritesPreview = false;
		}

		public bool ShowSpritesPreview { get; set; }
		public bool ShowContainerPreview { get; set; }
		public bool ShowAnimation { get; set; }
		public bool ShowFullImageButton { get; set; }
		public bool ShowHexEditor { get; set; }
		public bool ShowComboBox { get; set; }
		public bool ClearAnimationImages { get; set; }
		public bool ShowTextEditor { get; set; }
		public bool ShowRawStructureTextEditor { get; set; }
		public bool ShowLubDecompiler { get; set; }
		public bool ResetImagePreview { get; set; }
		public bool ShowImagePreview { get; set; }
		public bool ShowSoundPreview { get; set; }
		public bool CenterImagePreview { get; set; }
		public bool ShowMapExtractor { get; set; }
		public bool ShowRsm { get; set; }
		public bool ShowStr { get; set; }
		public bool ShowGnd { get; set; }
		//public bool ShowGatEditor { get; set; }
		public bool ShowGat { get; set; }
		public bool ShowDb { get; set; }
		public bool ShowEditSprite { get; set; }
		public bool ShowFolderPreview { get; set; }
		public bool ShowFolderStructurePreview { get; set; }
		public bool ShowGrfProperties { get; set; }

		public static PreviewDisplayUpdaterConfiguration operator -(PreviewDisplayConfiguration p1, PreviewDisplayConfiguration p2) {
			PreviewDisplayUpdaterConfiguration conf = new PreviewDisplayUpdaterConfiguration();
			foreach (PropertyInfo property in typeof (PreviewDisplayConfiguration).GetProperties()) {
				if (property.CanRead) {
					if (Boolean.Parse(property.GetValue(p1, null).ToString()) != Boolean.Parse(property.GetValue(p2, null).ToString())) {
						conf.AddUpdatedProperty(property.Name);
					}
				}
			}
			return conf;
		}
	}

	public class PreviewDisplayUpdaterConfiguration {
		private readonly List<string> _propertiesUpdated = new List<string>();

		public void AddUpdatedProperty(string property) {
			_propertiesUpdated.Add(property);
		}

		public bool HasValueChanged<T>(T item) where T : class {
			return _propertiesUpdated.Contains(typeof (T).GetProperties()[0].Name);
		}
	}
}