using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using GRF.Core;
using GRF.FileFormats.ActFormat;
using GRF.FileFormats.GatFormat;
using GRF.FileFormats.GndFormat;
using GRF.FileFormats.RsmFormat;
using GRF.FileFormats.RswFormat;
using GRF.FileFormats.SprFormat;
using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using GRF.Image;
using Utilities.Services;

namespace GRF.FileFormats {
	public static class FileFormatParser {
		private static readonly List<Type> _expandableTypes = new List<Type>();

		static FileFormatParser() {
			_expandableTypes.Add(typeof (String));
			_expandableTypes.Add(typeof (StrLayer));
		}

		public static List<Type> ExpandableTypes {
			get { return _expandableTypes; }
		}

		public static string DisplayObjectProperties(Object o) {
			Type type = o.GetType();
			var extension = Path.GetExtension(type.ToString());
			if (extension != null) return _displayObjectProperties(o, extension.Remove(0, 1));
			return "";
		}

		public static string DisplayObjectPropertiesFromEntry(GrfHolder grf, FileEntry entry) {
			StringBuilder builder = new StringBuilder();
			builder.AppendLine(DisplayObjectProperties(entry));
			string toRet = builder.ToString();

			while (toRet.EndsWith(Environment.NewLine)) {
				toRet = toRet.Remove(toRet.Length - Environment.NewLine.Length, Environment.NewLine.Length);
			}

			toRet += Environment.NewLine;
			return toRet;
		}

		public static string DisplayObjectPropertiesFromEntry(GrfHolder grf, string file) {
			return DisplayObjectPropertiesFromEntry(grf, grf.FileTable[file]);
		}

		private static string _displayObjectProperties(Object o, string parent, int level = 0) {
			StringBuilder sb = new StringBuilder();
			Type type = o.GetType();

			if (level > 3)
				return "";

			foreach (PropertyInfo p in type.GetProperties(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance)) {
				if (p.CanRead) {
					try {
						if (p.Name == "EncryptionKey") {
							continue;
						}

						object obj = p.GetValue(o, null);

						if (obj != null) {
							if (p.Name == "Key" && obj is String) {
								_addProperty(sb, level, parent, p.Name, BitConverter.ToString(EncodingService.DisplayEncoding.GetBytes((String) obj)));
							}
							else if (p.Name == "EncryptionKey") {
								// Ignored
							}
							else if (p.Name == "MainNode" && obj is Mesh) {
								_addProperty(sb, level, parent, p.Name, "[Ignored property]");
							}
							else if (p.Name == "Box" && obj is BoundingBox) {
								_addProperty(sb, level, parent, p.Name, "[Ignored property]");
							}
							else if (p.Name == "NewCompressedData" && obj is IList) {
								_addProperty(sb, level, parent, p.Name, "[Ignored property]");
							}
							else if (p.Name == "DataImage") {
								_addProperty(sb, level, parent, p.Name, "[Ignored property]");
							}
							else if (p.Name == "Files" && obj is IList) {
								_addProperty(sb, level, parent, p.Name, "[Ignored property]");
							}
							else if (obj is Int32 || obj is UInt32 || obj is Int16 || obj is UInt16 || obj is long || obj is ulong) {
								_addProperty(sb, level, parent, p.Name, obj.ToString());
							}
							else if (obj is byte) {
								_addProperty(sb, level, parent, p.Name, ((Byte) obj).ToString(CultureInfo.InvariantCulture));
							}
							else if (obj is Double) {
								_addProperty(sb, level, parent, p.Name, ((Double) obj).ToString(CultureInfo.InvariantCulture));
							}
							else if (obj is Single) {
								_addProperty(sb, level, parent, p.Name, ((Single) obj).ToString(CultureInfo.InvariantCulture));
							}
							else if (obj is String) {
								_addProperty(sb, level, parent, p.Name, ((String) obj).ToString(CultureInfo.InvariantCulture));
							}
							else if (obj is GrfImage) {
								_addProperty(sb, level, parent, p.Name, "GRF Image Format");
							}
							else if (obj is GrfColor) {
								_addProperty(sb, level, parent, p.Name, obj.ToString());
							}
							else if (obj is Boolean) {
								_addProperty(sb, level, parent, p.Name, ((Boolean) obj).ToString(CultureInfo.InvariantCulture));
							}
							else if (obj is Stream) {
								_addProperty(sb, level, parent, p.Name, "Stream");
							}
							else if (obj is Enum) {
								_addProperty(sb, level, parent, p.Name, obj.ToString());
							}
							else {
								if (obj is IList && ((IList) obj).Count > 0) {
									//sb.AppendLine(_addSpaces(level + 1) + parent + "." + p.Name + "...");
									_addProperty(sb, level, parent, p.Name, ((IList) obj).Count.ToString(CultureInfo.InvariantCulture));
									foreach (object oj in ((IList) obj)) {
										if (!_expandableTypes.Contains(oj.GetType()))
											break;

										_addProperty(sb, level + 1, "", "", oj.ToString());
									}
									sb.AppendLine();
								}
								else {
									if (obj is IList) {
										_addProperty(sb, level, parent, p.Name, ((IList) obj).Count.ToString(CultureInfo.InvariantCulture));
									}
									else {
										sb.AppendLine(_addSpaces(level) + parent + "." + p.Name + "...");
										string toAdd = _displayObjectProperties(obj, parent + "." + p.Name, level + 1);
										if (toAdd != "") {
											sb.Append(toAdd);
										}
										sb.AppendLine();
									}
								}
							}
						}
						else sb.AppendLine(_addSpaces(level) + parent + "." + p.Name + " = [Null value]");
					}
					catch {
					}
				}
			}
			return sb.ToString();
		}

		private static void _addProperty(StringBuilder sb, int level, string parent, string name, string value) {
			sb.AppendLine(_addSpaces(level) + ((parent == "" && name == "") ? "" : (parent + "." + name + " = ")) + value);
		}

		private static string _addSpaces(int level) {
			string toAdd = "";
			for (int i = 0; i < level; i++) {
				toAdd += "  ";
			}
			return toAdd;
		}
	}
}