using GRF.Core;
using Utilities;

namespace GRFEditor.WPF.PreviewTabs {
	public interface IFolderPreviewTab : IPreviewTab {
		void Load(GrfHolder grfData, TkPath entry);
	}
}