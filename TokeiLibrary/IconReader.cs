using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Utilities;

namespace TokeiLibrary {
	/// <summary>
	/// Provides static methods to read system icons for both folders and files.
	/// </summary>
	/// <example>
	/// <code>IconReader.GetFileIcon("c:\\general.xls");</code>
	/// </example>
	public class IconReader {
		#region FolderType enum

		/// <summary>
		/// Options to specify whether folders should be in the open or closed state.
		/// </summary>
		public enum FolderType {
			/// <summary>
			/// Specify open folder.
			/// </summary>
			Open = 0,
			/// <summary>
			/// Specify closed folder.
			/// </summary>
			Closed = 1
		}

		#endregion

		#region Size enum

		/// <summary>
		/// Options to specify the size of icons to return.
		/// </summary>
		public enum Size {
			/// <summary>
			/// Specify large icon - 32 pixels by 32 pixels.
			/// </summary>
			Large = 0,
			/// <summary>
			/// Specify small icon - 16 pixels by 16 pixels.
			/// </summary>
			Small = 1
		}

		#endregion

		public static Icon GetFileIcon(string name, bool small, bool linkOverlay) {
			NativeMethods.SHFILEINFO shfi = new NativeMethods.SHFILEINFO();
			uint flags = (uint) (NativeMethods.SHGFI.ICON | NativeMethods.SHGFI.USEFILEATTRIBUTES);

			if (linkOverlay) flags += (uint) NativeMethods.SHGFI.LINKOVERLAY;


			/* Check the size specified for return. */
			if (small) {
				flags += (uint) NativeMethods.SHGFI.SMALLICON; // include the small icon flag
			}
			else {
				flags += 2;  // include the large icon flag
			}

			NativeMethods.SHGetFileInfo(name,
				(uint) NativeMethods.FILE_ATTRIBUTE.NORMAL,
				ref shfi,
				(uint)Marshal.SizeOf(shfi),
				flags);


			// Copy (clone) the returned icon to a new object, thus allowing us 
			// to call DestroyIcon immediately
			Icon icon = (Icon)
								 Icon.FromHandle(shfi.hIcon).Clone();
			NativeMethods.DestroyIcon(shfi.hIcon); // Cleanup
			return icon;
		}

		/// <summary>
		/// Returns an icon for a given file - indicated by the name parameter.
		/// </summary>
		/// <param name="path">Pathname for file.</param>
		/// <param name="size">Large or small</param>
		/// <param name="showOverlay">Whether to include the link icon</param>
		/// <returns>System.Drawing.Icon</returns>
		public static Icon GetIcon(Size size, string fullName, bool isFile, bool isIntensiveSearch, bool showOverlay, bool isLink) {
			uint dwAttrs = 0;
			uint flags = 0;

			if (isLink) {
				flags += (uint)NativeMethods.SHGFI.LINKOVERLAY;
			}

			// Show overlay for files.
			if (showOverlay) {
				flags += (uint)NativeMethods.SHGFI.ADDOVERLAYS;
			}

			if (!showOverlay && !isIntensiveSearch) {
				flags += (uint)NativeMethods.SHGFI.USEFILEATTRIBUTES;
			}

			if (isFile) {
				dwAttrs += (uint)NativeMethods.FILE_ATTRIBUTE.NORMAL;
			}
			else {
				dwAttrs += (uint)NativeMethods.FILE_ATTRIBUTE.DIRECTORY;
			}

			// Get the folder icon.
			return _getIcon(fullName, size, dwAttrs, flags);
		}

		/// <summary>
		/// Gets the icon.
		/// </summary>
		/// <param name="path">Path to retrieve the icon.</param>
		/// <param name="dwAttrs">Attributes of the icon.</param>
		/// <param name="size">Size of the icon.</param>
		/// <param name="flags">Flags used when retrieving the icon.</param>
		/// <returns>Icon.</returns>
		private static Icon _getIcon(string path, Size iconSize, uint dwAttrs, uint flags) {
			flags += (uint)NativeMethods.SHGFI.SYSICONINDEX | (uint)NativeMethods.SHGFI.ICON;

			/* Check the size specified for return. */
			if (Size.Small == iconSize) {
				flags += (uint)NativeMethods.SHGFI.SMALLICON;
			}
			else {
				flags += (uint)NativeMethods.SHGFI.LARGEICON;
			}

			Icon icon = null;
			NativeMethods.SHFILEINFO shfi = new NativeMethods.SHFILEINFO();

			try {
				NativeMethods.SHGetFileInfo(path,
					dwAttrs,
					ref shfi,
					(uint)Marshal.SizeOf(shfi),
					flags);

				// Copy (clone) the returned icon to a new object, thus allowing us to clean-up properly.
				icon = (Icon)Icon.FromHandle(shfi.hIcon).Clone();
			}
			catch (ArgumentException ex) {
				string flagInfo = Enum.Format(typeof(NativeMethods.SHGFI), flags, "F");

				ex.Data["flags"] = flagInfo;
				throw;
			}
			finally {
				// Cleanup.
				NativeMethods.DestroyIcon(shfi.hIcon);
			}

			return icon;
		}
	}
}