using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utilities;
using Utilities.Extension;
using Utilities.Parsers.Lua;
using Utilities.Parsers.Lua.Structure;
using Utilities.Services;

namespace GRF.FileFormats.LubFormat.Preset {
	public class JobnameLubData {
		public Dictionary<int, string> Id2Sprite = new Dictionary<int, string>();
		public Dictionary<string, int> Sprite2Id = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		public Dictionary<string, string> Jobname = new Dictionary<string, string>();
		public Dictionary<string, int> NpcIdentity = new Dictionary<string, int>();
		public Dictionary<int, string> Id2Job = new Dictionary<int, string>();

		public JobnameLubData(MultiType rawJobnameData, MultiType rawNpcIdentityData) {
			var jobnameParser = new LuaParser(rawJobnameData.Data, true, p => new Lub(p).Decompile(), EncodingService.DisplayEncoding, EncodingService.DisplayEncoding);
			var npcidentityParser = new LuaParser(rawNpcIdentityData.Data, true, p => new Lub(p).Decompile(), EncodingService.DisplayEncoding, EncodingService.DisplayEncoding);

			Jobname = jobnameParser.Tables.FirstOrDefault(p => String.Compare("JobNameTable", p.Key, StringComparison.OrdinalIgnoreCase) == 0).Value.ToDictionary(p => p.Key.Trim('[', ']').Replace("jobtbl.", ""), p => p.Value);
			NpcIdentity = npcidentityParser.Tables.FirstOrDefault(p => String.Compare("jobtbl", p.Key, StringComparison.OrdinalIgnoreCase) == 0).Value.ToDictionary(p => p.Key, p => Int32.Parse(p.Value));

			// Associate the values properly
			foreach (var pair in Jobname) {
				int temp_i;

				if (NpcIdentity.TryGetValue(pair.Key, out temp_i) || Int32.TryParse(pair.Key, out temp_i)) {
					Id2Sprite[temp_i] = pair.Value.Trim('\"');
					Sprite2Id[pair.Value.Trim('\"')] = temp_i;
				}
			}

			foreach (var pair in NpcIdentity) {
				Id2Job[pair.Value] = pair.Key;
			}
		}
	}

	public class AccnameLubData {
		public Dictionary<int, string> Id2Sprite = new Dictionary<int, string>();
		public Dictionary<string, string> Accname = new Dictionary<string, string>();
		public Dictionary<string, int> AccessoryId = new Dictionary<string, int>();

		public AccnameLubData(MultiType rawAccnameData, MultiType rawAccessoryIdData) {
			var accnameParser = new LuaParser(rawAccnameData.Data, true, p => new Lub(p).Decompile(), EncodingService.DisplayEncoding, EncodingService.DisplayEncoding);
			var accessoryIdParser = new LuaParser(rawAccessoryIdData.Data, true, p => new Lub(p).Decompile(), EncodingService.DisplayEncoding, EncodingService.DisplayEncoding);

			Accname = accnameParser.Tables.FirstOrDefault(p => String.Compare("AccNameTable", p.Key, StringComparison.OrdinalIgnoreCase) == 0).Value.ToDictionary(p => p.Key.Trim('[', ']').Replace("ACCESSORY_IDs.", ""), p => p.Value.Substring(1));
			AccessoryId = accessoryIdParser.Tables.FirstOrDefault(p => String.Compare("ACCESSORY_IDs", p.Key, StringComparison.OrdinalIgnoreCase) == 0).Value.ToDictionary(p => p.Key, p => Int32.Parse(p.Value));

			// Associate the values properly
			foreach (var pair in Accname) {
				int temp_i;

				if (AccessoryId.TryGetValue(pair.Key, out temp_i) || Int32.TryParse(pair.Key, out temp_i)) {
					Id2Sprite[temp_i] = pair.Value.Trim('\"');
				}
			}
		}
	}

	public class SkillLubData {
		public class SkillInfoList {
			private List<int> _spAmount = new List<int>();
			private List<int> _apAmount = new List<int>();
			private List<int> _attackRange = new List<int>();
			private Dictionary<int, Tuple<int, int>> _skillScale = new Dictionary<int, Tuple<int, int>>();

			public string SkillName { get; set; }
			public string ConstantName { get; set; }
			public int MaxLv { get; set; }

			public List<int> SpAmount {
				get { return _spAmount; }
				set { _spAmount = value; }
			}

			public List<int> ApAmount {
				get { return _apAmount; }
				set { _apAmount = value; }
			}

			public bool bSeperateLv { get; set; }

			public List<int> AttackRange {
				get { return _attackRange; }
				set { _attackRange = value; }
			}

			public Dictionary<int, Tuple<int, int>> SkillScale {
				get { return _skillScale; }
				set { _skillScale = value; }
			}
		}

		public Dictionary<int, SkillInfoList> Id2SkillInfo = new Dictionary<int, SkillInfoList>();
		public Dictionary<string, int> SkillId = new Dictionary<string, int>();

		public SkillLubData(MultiType skillinfoData, MultiType skillidData) {
			var skillidParser = new LuaParser(skillidData.Data, true, p => new Lub(p).Decompile(), EncodingService.DisplayEncoding, EncodingService.DisplayEncoding);
			SkillId = skillidParser.Tables.FirstOrDefault(p => String.Compare("SKID", p.Key, StringComparison.OrdinalIgnoreCase) == 0).Value.ToDictionary(p => p.Key, p => Int32.Parse(p.Value));
			byte[] rawSkillinfoData;

			if (Methods.ByteArrayCompare(skillinfoData.Data, 0, 4, new byte[] { 0x1b, 0x4c, 0x75, 0x61 }, 0)) {
				Lub lub = new Lub(skillinfoData.Data);
				rawSkillinfoData = EncodingService.DisplayEncoding.GetBytes(lub.Decompile());
			}
			else {
				rawSkillinfoData = skillinfoData.Data;
			}

			LuaList list;

			using (LuaReader reader = new LuaReader(new MemoryStream(rawSkillinfoData), EncodingService.DisplayEncoding)) {
				list = reader.ReadAll();
			}

			LuaKeyValue itemVariable = list.Variables[0] as LuaKeyValue;

			if (itemVariable == null)
				return;

			foreach (LuaKeyValue skillEntry in ((LuaList)itemVariable.Value).Variables.OfType<LuaKeyValue>()) {
				string key_s = skillEntry.Key.Replace("[SKID.", "").Replace("]", "");
				int key;

				if (!SkillId.TryGetValue(key_s, out key) && !Int32.TryParse(key_s, out key)) {
					continue;
				}

				var skillinfoList_b = new SkillInfoList();
				Id2SkillInfo[key] = skillinfoList_b;

				skillinfoList_b.ConstantName = key_s;

				foreach (LuaKeyValue skillProperty in ((LuaList)skillEntry.Value).Variables.OfType<LuaKeyValue>()) {
					switch(skillProperty.Key) {
						case "SkillName":
							skillinfoList_b.SkillName = skillProperty.Value.ToString().Unescape(EscapeMode.RemoveQuote | EscapeMode.KeepAsciiCode);
							break;
						case "MaxLv":
							skillinfoList_b.MaxLv = Int32.Parse(skillProperty.Value.ToString());
							break;
						case "bSeperateLv":
							skillinfoList_b.bSeperateLv = Boolean.Parse(skillProperty.Value.ToString());
							break;
						case "SpAmount":
							skillinfoList_b.SpAmount = LuaList.ToIntList(skillProperty.Value);
							break;
						case "ApAmount":
							skillinfoList_b.ApAmount = LuaList.ToIntList(skillProperty.Value);
							break;
						case "AttackRange":
							skillinfoList_b.AttackRange = LuaList.ToIntList(skillProperty.Value);
							break;
						case "SkillScale":
							foreach (var skillScale in ((LuaList)skillProperty.Value).Variables.OfType<LuaKeyValue>()) {
								skillinfoList_b.SkillScale[Int32.Parse(skillScale.Key.Trim('[', ']'))] = new Tuple<int, int>(
									Int32.Parse(((LuaKeyValue)((LuaList)skillScale.Value).Variables[0]).Value.ToString()), 
									Int32.Parse(((LuaKeyValue)((LuaList)skillScale.Value).Variables[1]).Value.ToString()));
							}

							break;
						case "_NeedSkillList":
							break;
					}
				}
			}
		}
	}
}
