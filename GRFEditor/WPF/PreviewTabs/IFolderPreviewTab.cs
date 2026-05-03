using GRF.Core;
using GrfToWpfBridge.PreviewTabs;
using Utilities;

namespace GRFEditor.WPF.PreviewTabs {
	public interface IFolderPreviewTab : IPreviewTab {
		void Load(GrfHolder grfData, TkPath entry);
	}
}