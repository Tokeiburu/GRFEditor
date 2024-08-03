using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ErrorManager;
using GRF.Core;
using GRF.System;
using GRFEditor.ApplicationConfiguration;
using Microsoft.Win32;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Extension;
using Utilities.Hash;
using Utilities.Services;
using Binder = GrfToWpfBridge.Binder;
using Debug = Utilities.Debug;
using OpeningService = GRFEditor.Core.Services.OpeningService;
using Path = System.IO.Path;

namespace GRFEditor.WPF {
	/// <summary>
	/// Interaction logic for SettingsDialog.xaml
	/// </summary>
	public partial class SettingsDialog : TkWindow {
		public static Func<Action<Brush>[]> GetBackgroundScrollViewers = null;
		private readonly EditorMainWindow _editor;
		protected GrfHolder _grfData;

		public SettingsDialog(GrfHolder grfData, EditorMainWindow editor)
			: base("Settings", "settings.ico") {
			_grfData = grfData;
			_editor = editor;
			InitializeComponent();

			UIPanelPreviewBackgroundPick(_qcsBackground);

			_comboBoxCompression.Init();
			_comboBoxEncryption.Init();

			_comboBoxWarningLevel.SelectedIndex = (int)Configuration.WarningLevel;
			_textBoxMaxThreads.Text = GrfEditorConfiguration.MaximumNumberOfThreads.ToString(CultureInfo.InvariantCulture);

			_assShellAll.IsChecked = GrfEditorConfiguration.FileShellAssociated.HasFlags(FileAssociation.All);

			try {
				_tbConfig.Text = Path.GetFileName(GrfEditorConfiguration.ConfigAsker.ConfigFile);
				_tbPixelIndexation.Text = Methods.CanUseIndexed8 ? "Directly handled by GDI" : "Manually converted to Bgra32";
				_tbFramework.Text = RuntimeEnvironment.GetSystemVersion() + " - " + Methods.GetReadableRuntimeVersion();
				_tbRenderingMode.Text = Configuration.BestAvailableScaleMode.ToString();
			}
			catch {
			}

			if (grfData.IsOpened) {
				if (grfData.Header.Is(2, 0)) {
					_comboBoxFormat.SelectedIndex = 0;
				}
				else if (grfData.Header.Is(1, 3)) {
					_comboBoxFormat.SelectedIndex = 1;
				}
				else if (grfData.Header.Is(1, 2)) {
					_comboBoxFormat.SelectedIndex = 2;
				}
			}
			else {
				_comboBoxFormat.SelectedIndex = -1;
			}

			_comboBoxEncoding.Init(null,
								   new TypeSetting<int>(v => GrfEditorConfiguration.EncodingCodepage = v, () => GrfEditorConfiguration.EncodingCodepage),
								   new TypeSetting<Encoding>(v => EncodingService.DisplayEncoding = v, () => EncodingService.DisplayEncoding)
				);

			_comboBoxEncoding.EncodingChanged += (s, enc) => {
				if (!_editor.SetEncoding(enc.Encoding.CodePage)) {
					enc.Cancel = true;
				}
			};

			_comboBoxFormat.SelectionChanged += _comboBoxFormat_SelectionChanged;
			_comboBoxWarningLevel.SelectionChanged += _comboBoxWarningLevel_SelectionChanged;
			_assShellAll.Checked += _assShellAll_Checked;
			_assShellAll.Unchecked += _assShellAll_Unchecked;
			_textBoxMaxThreads.TextChanged += _textBoxMaxThreads_TextChanged;

			WpfUtils.AddMouseInOutEffects(_encodingImage);

			int row;
			_add(_gridTreeBehavior, row = 0, "Remember tree expansion for each GRF", "Saves the structure of the tree for the opened GRF to make it faster when opening the file again.", () => GrfEditorConfiguration.TreeBehaviorSaveExpansion);
			_add(_gridTreeBehavior, ++row, "Always expand specific folders", "Expand specific folders for all GRF or RGZ files when opening them.", () => GrfEditorConfiguration.TreeBehaviorExpandSpecificFolders, () => _buttonTreeExpandSpecific.IsEnabled = GrfEditorConfiguration.TreeBehaviorExpandSpecificFolders);
			_add(_gridTreeBehavior, ++row, "Move items in the tree by holding ALT", "Move folders in the main tree view (directories of the GRF) by holding down the ALT key. If disabled, it'll be the same behavior as Windows Explorer.", () => Configuration.TreeBehaviorUseAlt);
			_add(_gridTreeBehavior, ++row, "Remember last selected node", "If enabled, the last seleted node for a GRF will be reopend.", () => GrfEditorConfiguration.TreeBehaviorSelectLatest);
			_add(_gridTreeBehavior, ++row, "Translate paths in the tree", "If enabled, common paths will be translated in gray.", () => Configuration.TranslateTreeView);

			_add(_gridDebugger, row = 0, "Log any exceptions (debug.log)", "If enabled, all exceptions will be logged in the debug.log (found in the roaming folder).", () => Configuration.LogAnyExceptions);

			_add(_gridGeneral, row = 9, "CPU performance management", "This option monitors your CPU usage and it'll be used to dynamically change the number of threads doing work. " +
										 "The main purpose of this feature is to avoid situations where the CPU could reach 100% usage and hence lagging the entire system.", () => GrfEditorConfiguration.CpuPerformanceManagement, () => Settings.CpuMonitoringEnabled = GrfEditorConfiguration.CpuPerformanceManagement);
			_add(_gridGeneral, ++row, "Always open folder after extraction", "Prevents services from showing a file or folder in Windows Explorer after an extraction.", () => GrfEditorConfiguration.AlwaysOpenAfterExtraction, () => OpeningService.Enabled = GrfEditorConfiguration.AlwaysOpenAfterExtraction);
			_add(_gridGeneral, ++row, "Always reopen latest opened GRF", "Always reopen the most recently opened GRF when starting the application.", () => GrfEditorConfiguration.AlwaysReopenLatestGrf);
			//_add(_gridGeneral, ++row, "Use the opened GRF path to extract", "If enabled, the files extraced will be placed in the same folder as the GRF. They will be placed within the working directory of the application otherwise.", () => GrfEditorConfiguration.UseGrfPathToExtract);
			_add(_gridGeneral, ++row, "Enable windows ownership", "Makes the windows linked together, resulting in only being able to focus on one at a time. Disabling this features enables you to open multiple windows (as in tools).", () => Configuration.EnableWindowsOwnership);
			_add(_gridGeneral, ++row, "Lock added files", "If enabled, files added to a GRF will be locked (other processes won't be able to move, delete or modify them).", () => GrfEditorConfiguration.LockFiles);
			_add(_gridGeneral, ++row, "Add hash data to Thor files", "If enabled, a hash file will be added to Thor patches.", () => GrfEditorConfiguration.AddHashFileForThor);

			Binder.Bind(_cbOverrideExtractionPath, () => GrfEditorConfiguration.OverrideExtractionPath, delegate {
				_pbExtration.IsEnabled = GrfEditorConfiguration.OverrideExtractionPath;
			}, true);
			Binder.Bind(_pbExtration.TextBox, () => GrfEditorConfiguration.DefaultExtractingPath);
			WpfUtils.AddMouseInOutEffectsBox(_cbOverrideExtractionPath);

			//_add(_grid, ++row, "", "", () => );

			_assoc(_assGrf, ".grf", FileAssociation.Grf);
			_assoc(_assGpf, ".gpf", FileAssociation.Gpf);
			_assoc(_assRgz, ".rgz", FileAssociation.Rgz);
			_assoc(_assGrfKey, ".grfkey", FileAssociation.GrfKey);
			_assoc(_assThor, ".thor", FileAssociation.Thor);

			Binder.Bind(_comboBoxStyles, () => GrfEditorConfiguration.ThemeIndex, v => {
				GrfEditorConfiguration.ThemeIndex = v;
				Application.Current.Resources.MergedDictionaries.RemoveAt(Application.Current.Resources.MergedDictionaries.Count - 1);

				var path = "pack://application:,,,/" + Assembly.GetEntryAssembly().GetName().Name.Replace(" ", "%20") + ";component/WPF/Styles/";

				if (GrfEditorConfiguration.ThemeIndex == 0) {
					path += "StyleLightBlue.xaml";
				}
				else if (GrfEditorConfiguration.ThemeIndex == 1) {
					path += "StyleDark.xaml";
				}

				//Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(path, UriKind.RelativeOrAbsolute) });
				Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(path, UriKind.RelativeOrAbsolute) });

				ApplicationManager.OnThemeChanged();
				//ErrorHandler.HandleException("For the theme to apply properly, please restart the application.");
			});
			WpfUtils.AddMouseInOutEffectsBox(_assShellAll);

			_mViewer.SaveResourceMethod = v => GrfEditorConfiguration.Resources.SaveResources(v);
			_mViewer.LoadResourceMethod = () => GrfEditorConfiguration.Resources.LoadResources();
			GrfEditorConfiguration.Resources.Modified += () => _mViewer.LoadResourcesInfo();
			_mViewer.LoadResourcesInfo();
			_mViewer.CanDeleteMainGrf = false;
		}

		private void _assoc(CheckBox box, string ext, FileAssociation assoc) {
			WpfUtils.AddMouseInOutEffectsBox(box);
			box.ToolTip = new TextBlock { Text = "Associates GRF Editor with " + ext + " file extension.", MaxWidth = 350, TextWrapping = TextWrapping.Wrap };
			box.SetValue(ToolTipService.ShowDurationProperty, 30000);

			box.IsChecked = (GrfEditorConfiguration.FileShellAssociated & assoc) == assoc;

			box.Checked += delegate {
				try {
					GrfEditorConfiguration.FileShellAssociated |= assoc;
					ApplicationManager.AddExtension(Methods.ApplicationFullPath, ext.Substring(1).ToUpper(), ext, true);
				}
				catch (Exception err) {
					ErrorHandler.HandleException("Failed to associate the file extension", err, ErrorLevel.NotSpecified);
				}
			};

			box.Unchecked += delegate {
				try {
					GrfEditorConfiguration.FileShellAssociated &= ~assoc;
					ApplicationManager.RemoveExtension("grfeditor", ext);
				}
				catch (Exception err) {
					ErrorHandler.HandleException("Failed to remove the association of the file extension", err, ErrorLevel.NotSpecified);
				}
			};
		}

		private void _add(Grid gridTreeBehavior, int i, string content, string tooltip, Expression<Func<bool>> get, Action v = null) {
			while (i >= gridTreeBehavior.RowDefinitions.Count) {
				gridTreeBehavior.RowDefinitions.Add(new RowDefinition { Height = new GridLength(-1, GridUnitType.Auto) });
			}

			CheckBox box = new CheckBox { Content = content, VerticalAlignment = VerticalAlignment.Center };
			box.Margin = new Thickness(3);

			if (tooltip != null) {
				box.ToolTip = new TextBlock { Text = tooltip, MaxWidth = 350, TextWrapping = TextWrapping.Wrap };
				box.SetValue(ToolTipService.ShowDurationProperty, 30000);
			}

			box.SetValue(Grid.RowProperty, i);
			box.SetValue(Grid.ColumnProperty, 0);
			//box.SetValue(Grid.ColumnSpanProperty, 2);

			gridTreeBehavior.Children.Add(box);

			WpfUtils.AddMouseInOutEffectsBox(box);

			if (v != null)
				Binder.Bind(box, get, v, true);
			else
				Binder.Bind(box, get);
		}

		private void _assUncheck() {
			_assShellAll.Dispatch(delegate {
				_assShellAll.Unchecked -= _assShellAll_Unchecked;
				_assShellAll.IsChecked = false;
				_assShellAll.Unchecked += _assShellAll_Unchecked;
			});
		}

		private void _assShellAll_Checked(object sender, RoutedEventArgs e) {
			try {
				string dllName = Wow.Is64BitOperatingSystem ? "GrfMenuHandler64.dll" : "GrfMenuHandler32.dll";
				string filePath = Path.Combine(GrfEditorConfiguration.ProgramDataPath, dllName);

				try {
					byte[] data = ApplicationManager.GetResource(dllName);

					if (File.Exists(filePath)) {
						if (!Md5Hash.Compare(File.ReadAllBytes(filePath), data)) {
							try {
								File.WriteAllBytes(filePath, ApplicationManager.GetResource(dllName));
							}
							catch (Exception err) {
								_assUncheck();
								ErrorHandler.HandleException("Couldn't write the new COM DLL. This is most likely caused by a resource being used by Windows Explorer. Use the \"Remove all extensions\" button in the Settings page.", err);
								return;
							}
						}
					}
					else {
						File.WriteAllBytes(filePath, ApplicationManager.GetResource(dllName));
					}
				}
				catch (Exception err) {
					_assUncheck();
					ErrorHandler.HandleException(err);
					return;
				}

				new Thread(new ThreadStart(delegate {
					try {
						var registryKey = Registry.CurrentUser.CreateSubKey("Software\\Classes\\Applications\\GRF Editor.exe");
						if (registryKey == null)
							throw new Exception("Failed to create subkey for the association.");

						registryKey.SetValue(null, "\"" + Methods.ApplicationFullPath + "\"");

						ProcessStartInfo startInfo = new ProcessStartInfo();
						startInfo.FileName = "Regsvr32.exe";
						startInfo.Arguments = "/s \"" + filePath + "\"";
						startInfo.Verb = "runas";
						startInfo.UseShellExecute = false;
						startInfo.CreateNoWindow = true;
						Process.Start(startInfo).WaitForExit();

						GrfEditorConfiguration.FileShellAssociated |= FileAssociation.All;
					}
					catch (Exception err) {
						_assUncheck();
						ErrorHandler.HandleException(err);
					}
				})).Start();
			}
			catch (Exception err) {
				_assUncheck();
				ErrorHandler.HandleException(err);
			}
		}

		private void _assShellAll_Unchecked(object sender, RoutedEventArgs e) {
			try {
				string dllName = Wow.Is64BitOperatingSystem ? "GrfMenuHandler64.dll" : "GrfMenuHandler32.dll";
				string filePath = Path.Combine(GrfEditorConfiguration.ProgramDataPath, dllName);

				Debug.Ignore(() => File.Delete(filePath));
				Debug.Ignore(() => File.Delete(Path.Combine(GrfEditorConfiguration.ProgramDataPath, "msvcp100.dll")));
				Debug.Ignore(() => File.Delete(Path.Combine(GrfEditorConfiguration.ProgramDataPath, "msvcr100.dll")));

				new Thread(new ThreadStart(delegate {
					try {
						try {
							Registry.CurrentUser.DeleteSubKeyTree("\"Software\\Classes\\Applications\\GRF Editor.exe\"");
						}
						catch {
						}

						ProcessStartInfo startInfo = new ProcessStartInfo();
						startInfo.FileName = "Regsvr32.exe";
						startInfo.Arguments = "/u /s \"" + filePath + "\"";
						startInfo.Verb = "runas";
						startInfo.UseShellExecute = false;
						startInfo.CreateNoWindow = true;
						Process.Start(startInfo).WaitForExit();

						GrfEditorConfiguration.FileShellAssociated &= ~FileAssociation.All;
					}
					catch (Exception err) {
						_assShellAll.Dispatch(delegate {
							_assShellAll.Unchecked -= _assShellAll_Checked;
							_assShellAll.IsChecked = true;
							_assShellAll.Unchecked += _assShellAll_Checked;
						});
						ErrorHandler.HandleException(err);
					}
				})).Start();
			}
			catch (Exception err) {
				_assShellAll.Dispatch(delegate {
					_assShellAll.Unchecked -= _assShellAll_Checked;
					_assShellAll.IsChecked = true;
					_assShellAll.Unchecked += _assShellAll_Checked;
				});
				ErrorHandler.HandleException(err);
			}
		}

		protected void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		protected void _comboBoxWarningLevel_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			try {
				if (_comboBoxWarningLevel != null) {
					Configuration.WarningLevel = (ErrorLevel)_comboBoxWarningLevel.SelectedIndex;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		protected void _checkBoxOpenFolder_Checked(object sender, RoutedEventArgs e) {
			try {
				OpeningService.Enabled = true;
				GrfEditorConfiguration.AlwaysOpenAfterExtraction = true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		protected void _checkBoxOpenFolder_Unchecked(object sender, RoutedEventArgs e) {
			try {
				OpeningService.Enabled = false;
				GrfEditorConfiguration.AlwaysOpenAfterExtraction = false;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		protected void _comboBoxFormat_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_comboBoxFormat != null) {
				if (_grfData.IsOpened) {
					switch (_comboBoxFormat.SelectedIndex) {
						case 0:
							_grfData.Commands.ChangeVersion(2, 0);
							break;
						case 1:
							_grfData.Commands.ChangeVersion(1, 3);
							break;
						case 2:
							_grfData.Commands.ChangeVersion(1, 2);
							break;
					}
				}
				else {
					_comboBoxFormat.SelectedIndex = -1;
				}
			}
		}

		protected void _textBoxMaxThreads_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				GrfEditorConfiguration.MaximumNumberOfThreads = Int32.Parse(((TextBox)sender).Text);
				Settings.MaximumNumberOfThreads = GrfEditorConfiguration.MaximumNumberOfThreads;
			}
			catch {
			}
		}

		private void _buttonTreeExpandSpecific_Click(object sender, RoutedEventArgs e) {
			List<string> paths = Methods.StringToList(GrfEditorConfiguration.TreeBehaviorSpecificFolders);

			InputDialog dialog = new InputDialog("Enter the paths to expand for GRFs.", "Tree expansion", paths.Aggregate((a, b) => a + "\r\n" + b), false, false);
			dialog.Owner = this;
			dialog.TextBoxInput.AcceptsReturn = true;
			dialog.TextBoxInput.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			dialog.TextBoxInput.TextWrapping = TextWrapping.Wrap;
			dialog.TextBoxInput.Height = 200;
			dialog.TextBoxInput.MinHeight = 200;
			dialog.TextBoxInput.MaxHeight = 200;
			dialog.TextBoxInput.VerticalContentAlignment = VerticalAlignment.Top;

			if (dialog.ShowDialog() == true) {
				GrfEditorConfiguration.TreeBehaviorSpecificFolders = dialog.Input.Replace("\r", "").Replace("\n\n", "\n").Replace("\n", ",");
			}
		}

		private void _encodingImage_MouseDown(object sender, MouseButtonEventArgs e) {
			ErrorHandler.HandleException("If you're not sure about the encoding, just select Default. The encoding more or less matters, "
										 + "the only difference between encodings is how the file names will be shown. The actual data will not be affected by "
										 + "this.\n\nHowever, if you're working with Korean based file names, you should select the Korean encoding. The application will "
										 + "automatically convert the file names and if the file already exists, it will replace it (so be careful!).",
										 ErrorLevel.NotSpecified);
		}

		public static void UIPanelPreviewBackgroundPick(QuickColorSelector selector) {
			selector.PreviewColorChanged += _onSelectorOnPreviewColorChanged;
			selector.ColorChanged += _onSelectorOnPreviewColorChanged;
		}

		private static void _onSelectorOnPreviewColorChanged(object sender, Color value) {
			if (GrfEditorConfiguration.UIPanelPreviewBackground is SolidColorBrush) {
				if (((SolidColorBrush)GrfEditorConfiguration.UIPanelPreviewBackground).Color == value)
					return;
			}

			GrfEditorConfiguration.UIPanelPreviewBackground = new SolidColorBrush(value);

			if (GetBackgroundScrollViewers != null) {
				var scrollViewers = GetBackgroundScrollViewers();

				foreach (var viewer in scrollViewers) {
					viewer(GrfEditorConfiguration.UIPanelPreviewBackground);
				}
			}
		}

		public static void SetImagePreviewEvents(Rectangle previewPanel) {
			WpfUtils.AddMouseInOutEffects(previewPanel);
		}

		private void _buttonRemoveAllExtensions_Click(object sender, RoutedEventArgs e) {
			try {
				if (ErrorHandler.YesNoRequest("This operation will close Windows Explorer and reopen it. This is to prevent locked " +
											  "files within Explorer. You may also want to run the application with administrator " +
											  "privileges before using this option.\r\n\r\nDo you want to continue?", "Closing Windows Explorer")) {
					try {
						string[] possibleFilesToDelete = new string[] {
							Path.Combine(GrfEditorConfiguration.ProgramDataPath, "ContextMenuHandler64.dll"),
							Path.Combine(GrfEditorConfiguration.ProgramDataPath, "ContextMenuHandler32.dll"),
							Path.Combine(GrfEditorConfiguration.ProgramDataPath, "GrfMenuHandler64.dll"),
							Path.Combine(GrfEditorConfiguration.ProgramDataPath, "GrfMenuHandler32.dll"),
							Path.Combine(Methods.ApplicationPath, "ContextMenuHandler64.dll"),
							Path.Combine(Methods.ApplicationPath, "ContextMenuHandler32.dll"),
							Path.Combine(Methods.ApplicationPath, "GrfMenuHandler64.dll"),
							Path.Combine(Methods.ApplicationPath, "GrfMenuHandler32.dll"),
						};

						Process.GetProcessesByName("explorer").ToList().ForEach(p => p.Kill());

						foreach (string file in possibleFilesToDelete) {
							try {
								File.Delete(file);
							}
							catch {
							}
						}

						new Patch0002(2).PatchAppliaction();

						Close();
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
					}
					finally {
						Process.Start("explorer.exe");
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}