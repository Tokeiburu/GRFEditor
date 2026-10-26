using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TokeiLibrary.Shortcuts;

namespace GRFEditor.ApplicationConfiguration {
	public static class GrfEditorCommands {
		public static TkCommand Copy = ApplicationShortcut.Copy;
		public static TkCommand Paste = ApplicationShortcut.Paste;
		public static TkCommand New = ApplicationShortcut.New;
		public static TkCommand NewGpf = ApplicationShortcut.FromString(null, "Application.NewGpf");
		public static TkCommand NewRgz = ApplicationShortcut.FromString(null, "Application.NewRgz");
		public static TkCommand NewThor = ApplicationShortcut.FromString(null, "Application.NewThor");
		public static TkCommand Open = ApplicationShortcut.Open;
		public static TkCommand Save = ApplicationShortcut.Save;
		public static TkCommand CloseGrf = ApplicationShortcut.FromString("Ctrl-W", "Application.CloseGrf");
		public static TkCommand SaveAs = ApplicationShortcut.FromString(null, "Application.SaveAs");
		public static TkCommand SaveDefragment = ApplicationShortcut.FromString("Ctrl-Shift-S", "Application.GrfDefragment");
		public static TkCommand SaveCompact = ApplicationShortcut.FromString("Ctrl-Alt-S", "Application.GrfCompact");
		public static TkCommand SaveRepack = ApplicationShortcut.FromString(null, "Application.GrfRepack");
		public static TkCommand EncryptFileTable = ApplicationShortcut.FromString(null, "Application.EncryptFileTable");
		public static TkCommand Undo = ApplicationShortcut.Undo;
		public static TkCommand Redo = ApplicationShortcut.Redo;
		public static TkCommand NavigationForward = ApplicationShortcut.NavigationForward;
		public static TkCommand NavigationBackward = ApplicationShortcut.NavigationBackward;
		public static TkCommand Merge = ApplicationShortcut.FromString("Ctrl-M", "Application.ToolMerge");
		public static TkCommand Subtract = ApplicationShortcut.FromString("Ctrl-U", "Application.ToolSubtract");
		public static TkCommand Search = ApplicationShortcut.FromString("Ctrl-F", "Application.FocusSearch");
		public static TkCommand SearchList = ApplicationShortcut.FromString("Ctrl-Alt-F", "Application.FocusSearchItemsList");
		public static TkCommand ExtractRgz = ApplicationShortcut.FromString(null, "Application.ExtractRgz");
		public static TkCommand OpenContainerOptions = ApplicationShortcut.FromString("Ctrl-Shift-P", "Application.OpenContainerOptions");
		
		// Tools
		public static TkCommand ValidateGrf = ApplicationShortcut.FromString("Ctrl-Shift-A", "Application.ValidateGrf");
		public static TkCommand OpenProgramData = ApplicationShortcut.FromString(null, "Application.OpenProgramData");
		public static TkCommand ImportSprite = ApplicationShortcut.FromString(null, "Application.ImportSprite");
		public static TkCommand SpriteConverter = ApplicationShortcut.FromString("Ctrl-Shift-D", "Application.SpriteConverter");
		public static TkCommand FlatMapsMaker = ApplicationShortcut.FromString("Ctrl-Shift-M", "Application.FlatMapsMaker");
		public static TkCommand PatchMaker = ApplicationShortcut.FromString("Ctrl-Shift-P", "Application.PatchMaker");
		public static TkCommand Hash = ApplicationShortcut.FromString("Ctrl-Shift-H", "Application.Hash");
		public static TkCommand Encoding = ApplicationShortcut.FromString("Ctrl-Shift-E", "Application.Encoding");
		public static TkCommand GrfSearch = ApplicationShortcut.FromString("Ctrl-Shift-F", "Application.Search");
		public static TkCommand ImageConverter = ApplicationShortcut.FromString("Ctrl-Shift-I", "Application.ImageConverter");
		public static TkCommand GrfCL = ApplicationShortcut.FromString("Ctrl-Shift-X", "Application.GrfCL");
		public static TkCommand OpenGL = ApplicationShortcut.FromString(null, "Application.OpenGL");
		public static TkCommand Encryptor = ApplicationShortcut.FromString("Ctrl-Shift-R", "Application.Encryptor");
		public static TkCommand Settings = ApplicationShortcut.FromString("Ctrl-Shift-T", "Application.Settings");

		// FileEntry
		public static TkCommand OpenProperty = ApplicationShortcut.FromString("Ctrl-P", "FileEntry.OpenProperty");
		public static TkCommand Delete = ApplicationShortcut.Delete;
		public static TkCommand Rename = ApplicationShortcut.Rename;
		public static TkCommand Extract = ApplicationShortcut.FromString("Ctrl-E", "FileEntry.Extract");
		public static TkCommand ExtractTo = ApplicationShortcut.FromString(null, "FileEntry.ExtractTo");
		public static TkCommand SelectInExplorer = ApplicationShortcut.FromString("Ctrl-R", "FileEntry.SelectInExplorer");
		public static TkCommand OpenFile = ApplicationShortcut.FromString("Ctrl-J", "FileEntry.OpenFile");
		public static TkCommand ExportSelection = ApplicationShortcut.FromString(null, "FileEntry.ExportSelection");
		public static TkCommand FindUsage = ApplicationShortcut.FromString("Ctrl-I", "FileEntry.FindUsage");
		public static TkCommand GrfeEncrypt = ApplicationShortcut.FromString("Ctrl-L", "FileEntry.GrfeEncrypt");
		public static TkCommand GrfeDecrypt = ApplicationShortcut.FromString("Ctrl-Shift-L", "FileEntry.GrfeDecrypt");
		public static TkCommand GravityDecrypt = ApplicationShortcut.FromString("Ctrl-G", "FileEntry.GravityDecrypt");
		public static TkCommand SetKey = ApplicationShortcut.FromString("Ctrl-R", "FileEntry.SetKey");

		public static TkCommand ExtractRootFiles = ApplicationShortcut.FromString(null, "FileEntry.ExtractRootFiles");
		public static TkCommand ExtractRootFilesTo = ApplicationShortcut.FromString(null, "FileEntry.ExtractRootFilesTo");
		public static TkCommand TreeAdd = ApplicationShortcut.FromString(null, "FileEntry.TreeAdd");
		public static TkCommand TreeNewFolder = ApplicationShortcut.FromString(null, "FileEntry.TreeNewFolder");

		public static TkCommand GCCleanup = ApplicationShortcut.FromString("Ctrl-Shift-D0", "Application.GCCleanup");
	}
}
