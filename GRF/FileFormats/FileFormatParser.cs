using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using GRF.Core;
using GRF.FileFormats.RsmFormat;
using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using GRF.Image;
using Utilities;
using Utilities.Services;

namespace GRF.FileFormats {
	public static class FileFormatParser {
		public static string DisplayObjectProperties(Object o) {
			Type type = o.GetType();
			var extension = Path.GetExtension(type.ToString());
			if (extension != null) {
				ObjectParser parser = new ObjectParser(o);
				return parser.Print(3);
			}
			return "";
		}

		public static string DisplayObjectPropertiesFromEntry(FileEntry entry) {
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
			return DisplayObjectPropertiesFromEntry(grf.FileTable[file]);
		}
	}

	public class ObjectParserConfig {
		public bool LoadFields { get; set; } = true;
		public bool ExploreAllClasses { get; set; }
		public List<Type> ExplorableClasses = new List<Type>();
		public bool ExploreAllLists { get; set; }
		public List<Type> ExploreableListTypes = new List<Type>();
		public Dictionary<Type, Func<object, string>> TypeValueOverrides = new Dictionary<Type, Func<object, string>>();

		public ObjectParserConfig Clone() {
			ObjectParserConfig ret = new ObjectParserConfig();
			ret.ExploreAllClasses = ExploreAllClasses;
			ret.ExploreAllLists = ExploreAllLists;
			ret.ExplorableClasses = ExplorableClasses.ToList();
			ret.ExploreableListTypes = ExploreableListTypes.ToList();

			foreach (var entry in TypeValueOverrides)
				ret.TypeValueOverrides[entry.Key] = entry.Value;

			return ret;
		}
	}

	public class ObjectParser {
		private static ObjectParserConfig _defaultConfigSetting;

		static ObjectParser() {
			_defaultConfigSetting = new ObjectParserConfig();
			_defaultConfigSetting.LoadFields = false;
			_defaultConfigSetting.ExploreAllClasses = false;
			_defaultConfigSetting.ExploreAllLists = false;
			_defaultConfigSetting.ExploreableListTypes.Add(typeof(string));
			_defaultConfigSetting.ExploreableListTypes.Add(typeof(StrLayer));

			_defaultConfigSetting.TypeValueOverrides[typeof(GrfColor)] = o => ((GrfColor)o).ToString();
			_defaultConfigSetting.TypeValueOverrides[typeof(GrfImage)] = o => "[GRF Image Format]";
			_defaultConfigSetting.TypeValueOverrides[typeof(BoundingBox)] = o => "[Ignored property]";
		}

		private ObjectParserConfig _configSetting;

		public object Object;
		public Type Type;
		public string FriendlyName;
		public string PropertyName;
		public string TypeName;
		public string Value;
		public string FullPath;
		public MemberInfo MemberInfo;
		public bool IsField => MemberInfo is FieldInfo;
		public bool IsProperty => MemberInfo is PropertyInfo;
		public int Priority;

		public ObjectParser Parent;
		public List<ObjectParser> Children = new List<ObjectParser>();
		public bool IsEvaluated;

		public ObjectParser(object obj, ObjectParserConfig config) : this() {
			_configSetting = config;
			Object = obj;
			Type = Object?.GetType();
			Evaluate();
		}

		public ObjectParser(object obj) : this() {
			Object = obj;
			Type = Object?.GetType();
			Evaluate();
		}

		private ObjectParser() {
			_configSetting = _defaultConfigSetting;
		}

		private ObjectParser(ObjectParserConfig config) {
			_configSetting = config;
		}

		public ObjectParser Evaluate() {
			if (IsEvaluated)
				return this;

			PropertyName = MemberInfo?.Name ?? Path.GetExtension(Type.ToString()).Substring(1);
			FriendlyName = Methods.CleanPropertyName(PropertyName);
			TypeName = Object?.GetType().ToString() ?? "[Null value]";
			FullPath = (Parent != null ? Parent.FullPath + "." : "") + PropertyName;
			bool hasChildren = false;

			if (MemberInfo != null) {
				Priority = 1;

				if (Object == null) {
					Value = "[Null value]";
				}
				else if (_configSetting.TypeValueOverrides.TryGetValue(Type, out Func<object, string> v)) {
					Value = v(Object);
				}
				else if (_configSetting.TypeValueOverrides.TryGetValue(Type.BaseType, out Func<object, string> v2)) {
					Value = v2(Object);
				}
				else if (PropertyName == "Key" && Object is String) {
					Value = "\"" + BitConverter.ToString(EncodingService.DisplayEncoding.GetBytes((String)Object)) + "\"";
				}
				else if (PropertyName == "EncryptionKey") {
					Value = "[Ignored property]";
				}
				else if (PropertyName == "MainNode" && Object is Mesh) {
					Value = "[Ignored property]";
				}
				else if (PropertyName == "NewCompressedData" && Object is IList) {
					Value = "[Ignored property]";
				}
				else if (PropertyName == "DataImage") {
					Value = "[Ignored property]";
				}
				else if (TypeName.EndsWith(".Image") || TypeName.EndsWith(".ImageSource")) {
					Value = "[Ignored property]";
				}
				else if (PropertyName == "Files" && Object is IList) {
					Value = "[Ignored property]";
				}
				else if (
					Object is UInt32 || Object is Int32 || Object is Int16 || Object is UInt16 || Object is long || Object is ulong) {
					Value = Object.ToString();
				}
				else if (Object is Byte) {
					Value = ((Byte)Object).ToString(CultureInfo.InvariantCulture);
				}
				else if (Object is Double) {
					Value = ((Double)Object).ToString(CultureInfo.InvariantCulture);
				}
				else if (Object is Single) {
					Value = ((Single)Object).ToString(CultureInfo.InvariantCulture);
				}
				else if (Object is Boolean) {
					Value = ((Boolean)Object).ToString(CultureInfo.InvariantCulture);
				}
				else if (Object is String) {
					Value = ((String)Object).ToString(CultureInfo.InvariantCulture);
				}
				else if (Object is Stream) {
					Value = "Stream";
				}
				else if (Object is Enum) {
					Value = Object.ToString();
				}
				else {
					if (Object is IList list) {
						Priority = 2;

						Value = list.Count.ToString();
						hasChildren = list.Count > 0;

						if (hasChildren) {
							foreach (var listChild in list) {
								if (!_configSetting.ExploreAllLists && !_configSetting.ExploreableListTypes.Contains(listChild.GetType()))
									break;

								ObjectParser child = new ObjectParser(_configSetting);
								child.Object = listChild;
								child.Type = listChild?.GetType();
								child.MemberInfo = null;
								child.Parent = this;

								if (_configSetting.ExploreableListTypes.Contains(listChild.GetType())) {
									child.Value = child.Object.ToString();
									child.IsEvaluated = true;
								}

								Children.Add(child);
							}
						}

						IsEvaluated = true;
						return this;
					}
					else {
						hasChildren = true;
					}
				}
			}
			else {
				if (_configSetting.TypeValueOverrides.TryGetValue(Type, out Func<object, string> v)) {
					Value = v(Object);
				}
				else if (_configSetting.TypeValueOverrides.TryGetValue(Type.BaseType, out Func<object, string> v2)) {
					Value = v2(Object);
				}
				else {
					hasChildren = true;
				}
			}

			if (hasChildren) {
				// Value can be explored further

				foreach (PropertyInfo p in Type.GetProperties(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.GetIndexParameters().Length == 0)) {
					try {
						var cObject = p.GetValue(Object, null);

						ObjectParser child = new ObjectParser(_configSetting);
						child.Object = cObject;
						child.Type = cObject?.GetType();
						child.MemberInfo = p;
						child.Parent = this;
						child.IsEvaluated = false;
						Children.Add(child);
					}
					catch {
					}
				}

				if (_configSetting.LoadFields) {
					foreach (FieldInfo f in Type.GetFields(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance)) {
						try {
							var cObject = f.GetValue(Object);

							ObjectParser child = new ObjectParser(_configSetting);
							child.Object = cObject;
							child.Type = cObject?.GetType();
							child.MemberInfo = f;
							child.Parent = this;
							child.IsEvaluated = false;
							Children.Add(child);
						}
						catch {
						}
					}
				}

				if (Children.Count > 0)
					Priority = 3;
			}

			IsEvaluated = true;
			return this;
		}

		public string Print(int maxLevel) {
			StringBuilder b = new StringBuilder();

			foreach (var child in Children) {
				child._print("", 0, maxLevel, b);
			}

			return b.ToString();
		}

		private void _print(string indent, int currentLevel, int maxLevel, StringBuilder b) {
			if (!IsEvaluated) {
				Evaluate();
			}

			b.Append(indent);

			if (!String.IsNullOrEmpty(FullPath))
				b.Append(FullPath);

			if (Children.Count > 0) {
				b.AppendLine("...");

				if (currentLevel >= maxLevel)
					return;

				foreach (var child in Children) {
					child._print(indent + "  ", currentLevel + 1, maxLevel, b);
				}

				b.AppendLine();
				return;
			}
			else if (Value == null) {
				b.AppendLine("...");
			}
			else {
				if (String.IsNullOrEmpty(FullPath))
					b.AppendLine(Value);
				else
					b.AppendLine(" = " + Value);
			}
		}

		public override string ToString() {
			if (FullPath == null && Value != null)
				return Value;

			return FullPath + (Value != null ? " | " + Value : "");
		}
	}
}