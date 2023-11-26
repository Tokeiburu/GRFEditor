using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Utilities.Parsers.Lua.Structure;

namespace Utilities.Parsers.Lua {
	public class LuaParser {
		public static bool IsLub(byte[] data) {
			if (data.Length < 4)
				return false;

			string magic = Encoding.ASCII.GetString(data, 0, 4);

			if (magic != Encoding.ASCII.GetString(new byte[] { 0x1b, 0x4c, 0x75, 0x61 }, 0, 4))
				return false;
			return true;
		}

		public readonly Dictionary<string, Dictionary<string, string>> Tables = new Dictionary<string, Dictionary<string, string>>();

		public LuaParser(byte[] data, bool autoDecompile, Func<byte[], string> decompiler, Encoding sourceEncoding, Encoding destEncoding) {
			if (IsLub(data)) {
				if (autoDecompile) {
					data = sourceEncoding.GetBytes(decompiler(data));
				}
				else {
					throw new Exception("The file is compiled. Set the autoDecompile parameter to true.");
				}
			}

			using (var reader = new LuaReader(new MemoryStream(data), sourceEncoding)) {
				var tables = reader.ReadAll();

				foreach (LuaKeyValue keyValue in tables.Variables.OfType<LuaKeyValue>()) {
					Tables.Add(keyValue.Key, GetDictionary(keyValue.Value as LuaList, sourceEncoding, destEncoding));
				}
			}
		}

		public void Add(string tableKey, string key, string value) {
			Tables[tableKey][key] = value;
		}

		public void Write(string file, Encoding encoding) {
			bool isInt;

			using (StreamWriter stream = new StreamWriter(file, false, encoding)) {
				foreach (var keyPairDico in Tables) {
					isInt = false;

					foreach (var keyPair in keyPairDico.Value) {
						int ival;

						if (Int32.TryParse(keyPair.Value, out ival)) {
							isInt = true;
						}

						break;
					}

					stream.WriteLine(keyPairDico.Key + " = {");
					if (isInt) {
						foreach (var keyPair in keyPairDico.Value.OrderBy(p => Int32.Parse(p.Value))) {
							stream.WriteLine("\t" + keyPair.Key + " = " + keyPair.Value + ",");
						}
					}
					else {
						foreach (var keyPair in keyPairDico.Value) {
							stream.WriteLine("\t" + keyPair.Key + " = " + keyPair.Value + ",");
						}
					}
					stream.WriteLine("}");
				}
			}
		}

		public Dictionary<string, string> GetDictionary(LuaList list, Encoding source, Encoding destination) {
			Dictionary<string, string> dico = new Dictionary<string, string>();

			if (source.CodePage == destination.CodePage) {
				foreach (var item in list.Variables.Cast<LuaKeyValue>()) {
					dico[item.Key] = item.Value.ToString();
				}

				return dico;
			}

			foreach (var item in list.Variables.Cast<LuaKeyValue>()) {
				dico[item.Key] = destination.GetString(source.GetBytes(item.Value.ToString()));
			}

			return dico;
		}
	}
}
