using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using Microsoft.Win32;
using TokeiLibrary;
using Utilities;
using Utilities.Extension;

namespace GRFEditor.ApplicationConfiguration {
	/// <summary>
	/// Class used to update GRFE's software without breaking settings.
	/// </summary>
	public class SelfPatcher {
		public static List<SelfPatch> Patches = new List<SelfPatch>();

		public static readonly SelfPatch Patch0000 = new Patch0000(0);
		public static readonly SelfPatch Patch0001 = new Patch0001(1);
		public static readonly SelfPatch Patch0002 = new Patch0002(2);
		public static readonly SelfPatch Patch0003 = new Patch0003(3);
		public static readonly SelfPatch Patch0004 = new Patch0004(4);
		public static readonly SelfPatch Patch0005 = new Patch0005(5);

		static SelfPatcher() {
			Patches = Patches.OrderBy(p => p.PatchId).ToList();
		}

		public static void SelfPatch() {
			int currentPatchId = GrfEditorConfiguration.PatchId;

			foreach (SelfPatch patch in Patches) {
				if (patch.PatchId >= currentPatchId) {
					patch.PatchAppliaction();
					currentPatchId = patch.PatchId + 1;
				}
			}

			GrfEditorConfiguration.PatchId = currentPatchId;
		}
	}

	public abstract class SelfPatch {
		private readonly int _patchId;

		protected SelfPatch(int patchId) {
			_patchId = patchId;

			SelfPatcher.Patches.Add(this);
		}

		public int PatchId {
			get { return _patchId; }
		}

		public abstract bool PatchAppliaction();

		public void Safe(Action action) {
			try {
				action();
			}
			catch {
			}
		}
	}

	public class Patch0005 : SelfPatch {
		public Patch0005(int patchId)
			: base(patchId) {
		}

		public override bool PatchAppliaction() {
			try {
				GrfEditorConfiguration.ConfigAsker.RetrieveSetting(() => GrfEditorConfiguration.FlatMapsMakerOutputMapsPath).Reset();
				GrfEditorConfiguration.ConfigAsker.RetrieveSetting(() => GrfEditorConfiguration.FlatMapsMakerOutputTexturesPath).Reset();
				GrfEditorConfiguration.ConfigAsker.RetrieveSetting(() => GrfEditorConfiguration.FlatMapsMakerInputTexturesPath).Reset();
				GrfEditorConfiguration.ConfigAsker.RetrieveSetting(() => GrfEditorConfiguration.FlatMapsMakerInputMapsPath).Reset();
			}
			catch {
				return false;
			}

			return true;
		}
	}

	public class Patch0004 : SelfPatch {
		public Patch0004(int patchId) : base(patchId) {
		}

		public override bool PatchAppliaction() {
			try {
				//GrfEditorConfiguration.UseGrfPathToExtract = true;
				GrfEditorConfiguration.UIPanelPreviewBackground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
			}
			catch {
				return false;
			}

			return true;
		}
	}

	public class Patch0003 : SelfPatch {
		public Patch0003(int patchId) : base(patchId) {
		}

		public override bool PatchAppliaction() {
			try {
				GrfEditorConfiguration.UIPanelPreviewBackground = new SolidColorBrush(Colors.Transparent);
			}
			catch {
				return false;
			}

			return true;
		}
	}

	public class Patch0002 : SelfPatch {
		public Patch0002(int patchId) : base(patchId) {
		}

		public override bool PatchAppliaction() {
			try {
				Safe(() => _removeExtension(".grf"));
				Safe(() => _removeExtension(".grfkey"));
				Safe(() => _removeExtension(".gpf"));
				Safe(() => _removeExtension(".rgz"));
				Safe(() => _removeExtension(".spr"));
				Safe(() => _removeExtension(".thor"));
				Safe(() => ApplicationManager.RemoveContextMenu("grfeditor", ".grf"));
				Safe(() => ApplicationManager.RemoveContextMenu("grfeditor", ".grfkey"));
				Safe(() => ApplicationManager.RemoveContextMenu("grfeditor", ".gpf"));
				Safe(() => ApplicationManager.RemoveContextMenu("grfeditor", ".rgz"));
				Safe(() => ApplicationManager.RemoveContextMenu("grfeditor", ".spr"));
				Safe(() => ApplicationManager.RemoveContextMenu("grfeditor", ".thor"));
				Safe(() => _removeHKCU(@"Software\Classes\.grf"));
				Safe(() => _removeHKCU(@"Software\Classes\.grfkey"));
				Safe(() => _removeHKCU(@"Software\Classes\.gpf"));
				Safe(() => _removeHKCU(@"Software\Classes\.rgz"));
				Safe(() => _removeHKCU(@"Software\Classes\.spr"));
				Safe(() => _removeHKCU(@"Software\Classes\.thor"));
				Safe(() => _removeHKCU(@"Software\Classes\Applications\GRF Editor.exe.rgz"));
				Safe(() => _removeHKCU(@"Software\Classes\Applications\GRF Editor.exe.grf"));
				Safe(() => _removeHKCU(@"Software\Classes\Applications\GRF Editor.exe.gpf"));
				Safe(() => _removeHKCU(@"Software\Classes\Applications\GRF Editor.exe.grfkey"));
				Safe(() => _removeHKCU(@"Software\Classes\Applications\GRF Editor.exe.spr"));
				Safe(() => _removeHKCU(@"Software\Classes\Applications\GRF Editor.exe.thor"));
				Safe(() => _removeHKCU(@"Software\Classes\Applications\GRF Editor.exe"));

				GrfEditorConfiguration.FileShellAssociated = 0;

				Safe(ApplicationManager.RefreshExplorer);
			}
			catch {
				return false;
			}

			return true;
		}

		private void _removeHKCU(string path) {
			Registry.CurrentUser.DeleteSubKeyTree(path);
		}

		private void _removeExtension(string extension) {
			Registry.ClassesRoot.DeleteSubKeyTree("grfeditor" + extension);
			Registry.ClassesRoot.DeleteSubKeyTree(extension);
		}
	}

	public class Patch0001 : SelfPatch {
		public Patch0001(int patchId) : base(patchId) {
		}

		public override bool PatchAppliaction() {
			try {
				_removeExtension(".grfkey");
				_removeExtension(".grf");
				_removeExtension(".gpf");
				_removeExtension(".rgz");

				try {
					ApplicationManager.RemoveContextMenu("grfeditor", ".grf");
				}
				catch {
				}

				try {
					ApplicationManager.RemoveContextMenu("grfeditor", ".gpf");
				}
				catch {
				}

				try {
					ApplicationManager.RemoveContextMenu("grfeditor", ".rgz");
				}
				catch {
				}

				if (GrfEditorConfiguration.FileShellAssociated.HasFlags(FileAssociation.Grf)) {
					ApplicationManager.AddExtension(Methods.ApplicationFullPath, "GRF", ".grf", true);
				}

				if (GrfEditorConfiguration.FileShellAssociated.HasFlags(FileAssociation.Gpf)) {
					ApplicationManager.AddExtension(Methods.ApplicationFullPath, "GPF", ".gpf", true);
				}

				if (GrfEditorConfiguration.FileShellAssociated.HasFlags(FileAssociation.Grf)) {
					ApplicationManager.AddExtension(Methods.ApplicationFullPath, "RGZ", ".rgz", true);
				}

				if (GrfEditorConfiguration.FileShellAssociated.HasFlags(FileAssociation.GrfKey)) {
					ApplicationManager.AddExtension(Methods.ApplicationFullPath, "Grf Key", ".grfkey", false);
				}
			}
			catch {
				return false;
			}

			return true;
		}

		private void _removeExtension(string extension) {
			try {
				Registry.ClassesRoot.DeleteSubKeyTree("grfeditor" + extension);
				Registry.ClassesRoot.DeleteSubKeyTree(extension);
			}
			catch {
			}
		}
	}

	public class Patch0000 : SelfPatch {
		public Patch0000(int patchId) : base(patchId) {
		}

		public override bool PatchAppliaction() {
			try {
				string configFile = Path.Combine(Methods.ApplicationPath, "config.txt");

				if (File.Exists(configFile)) {
					GrfEditorConfiguration.ConfigAsker.Merge(new ConfigAsker(Path.Combine(Methods.ApplicationPath, "config.txt")));
				}
			}
			catch {
				return false;
			}

			return true;
		}
	}
}