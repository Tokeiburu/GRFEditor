﻿using System.Collections.Generic;
using System.Text;
using GRF.FileFormats.LubFormat.Types;
using GRF.System;
using Utilities.Extension;

namespace GRF.FileFormats.LubFormat.VM {
	public static partial class OpCodes {
		#region Nested type: GetGlobal

		public class GetGlobal : AbstractInstruction {
			public GetGlobal() {
				Mode = EncodedMode.ABx;
			}

			protected override void _execute(LubFunction function) {
				try {
					_dump1 = function.Constants[Registers[1]];
					_dump1.Source = LubSourceType.Global;
					function.Stack[Registers[0]] = _dump1;
				}
				catch {
					LubErrorHandler.Handle("Failed to retrieve a global variable.", LubSourceError.CodeDecompiler);
				}
			}
		}

		#endregion

		#region Nested type: GetTable

		public class GetTable : AbstractInstruction {
			public GetTable() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				// GETTABLE	A B C	R(A) := R(B)[RK(C)]
				// R(A) is overwritten whatever the case
				// By putting a LubOutput, we fix the result so 
				// that it doesn't get misinterpreted by other instructions

				// R(B) is the table being accessed; it doesn't have
				// to be a table, it can be anything really. If it's a 
				// local variable, GetKey will get the variable name.

				// RK(C) is the indexer of the table. There are many ways
				// to access a table, here are the handled cases :
				// 1 - table["x"] (equivalent to table.x, preferred)
				// 2 - table[0]
				// 3 - table[x] (must be global)
				// 4 - local x; table[x] (must be a reference type)

				string table = GetKey(function.Stack[Registers[1]]).ToString();

				// Cases 3 and 4 are handled by GetKey
				ILubObject acces = GetKey(RegOrK(Registers[2], function));

				LubValueType accessor = (LubValueType) acces;

				if (RegOrK(Registers[2], function) is LubValueType &&
				    accessor.Source == LubSourceType.Constant &&
				    accessor is LubString) {
					// Case 1 handled
					table = table + "." + accessor;
				}
				else {
					// Case 2 (and maybe others) handled
					table = table + "[" + accessor + "]";
				}

				function.Stack[Registers[0]] = new LubOutput(table);
			}
		}

		#endregion

		#region Nested type: GetUpVal

		public class GetUpVal : AbstractInstruction {
			public GetUpVal() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				function.Stack[Registers[0]] = function.UpValues[Registers[1]];
			}
		}

		#endregion

		#region Nested type: NewTable

		public class NewTable : AbstractInstruction {
			public NewTable() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				//if (Registers[0] >= function.NumberOfParameters && Registers[0] < function.LocalVariables.Count && GetVal(function.Stack[Registers[0]]) == null) {
				//	LubReferenceType local = function.LocalVariables[Registers[0]];
				//
				//	Append = true;
				//	StringBuilder builder = new StringBuilder();
				//
				//	function.Stack[Registers[0]] = new LubDictionary(Registers[1], Registers[2]);
				//
				//	function.Instantiated[local.Key] = true;
				//	local.Value = function.Stack[Registers[0]];
				//	function.Stack[Registers[0]] = local;
				//	builder.AppendIndent(function.FunctionLevel);
				//	builder.AppendLine("local " + local.Key + " = " + local.Value);
				//	LuaCode = builder.ToString();
				//}
				//else {
					function.Stack[Registers[0]] = new LubDictionary(Registers[1], Registers[2]);
				//}
			}
		}

		#endregion

		//public class SetGlobal : AbstractInstruction, IAssigning {

		#region Nested type: SetGlobal

		public class SetGlobal : AbstractInstruction, IAssigning {
			public SetGlobal() {
				Mode = EncodedMode.ABx;
			}

			protected override void _execute(LubFunction function) {
				Append = true;
				StringBuilder builder = new StringBuilder();

				function.Decompiler.GlobalVariables.SetKeyValue((LubString) function.Constants[Registers[1]], RegOutput(Registers[0], function));
				LubKeyValue globalVariable = function.Decompiler.GlobalVariables.GetKeyValue((LubString) function.Constants[Registers[1]]);

				if (globalVariable.Value is LubFunction) {
					builder.AppendLine();

					if (Settings.LubDecompilerSettings.AppendFunctionId) {
						builder.AppendLine("-- Function #" + ++function.Decompiler.FunctionDecompiledCount);
					}
				}

				globalVariable.Print(builder, function.FunctionLevel);
				builder.AppendLine();
				LuaCode = builder.ToString();
			}
		}

		#endregion

		#region Nested type: SetList50

		public class SetList50 : AbstractInstruction {
			public SetList50() {
				Mode = EncodedMode.ABx;
			}

			protected override void _execute(LubFunction function) {
				int fPf = 32;

				int stackFrom = Registers[0] + 1;
				int stackTo = Registers[1] % fPf + 1;

				LubDictionary dictionary = (LubDictionary) GetVal(function.Stack[Registers[0]]);

				List<ILubObject> items = new List<ILubObject>(stackTo);

				for (int i = 0; i < stackTo; i++) {
					items.Add(RegOutput(stackFrom + i, function));
				}

				dictionary.AddList(items);
			}
		}

		#endregion

		#region Nested type: SetList51

		public class SetList51 : AbstractInstruction {
			public SetList51() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				//int fPf = 50;

				//int stackFrom = Registers[0] + 1;
				int count = Registers[1];

				if (Registers[1] == 0)
					LubErrorHandler.Handle("Non defined behavior for SetList51.", LubSourceError.CodeDecompiler);

				if (Registers[2] == 0)
					LubErrorHandler.Handle("Non defined behavior for SetList51.", LubSourceError.CodeDecompiler);

				LubDictionary dictionary = (LubDictionary) GetVal(function.Stack[Registers[0]]);

				List<ILubObject> items = new List<ILubObject>(count);

				for (int i = 0; i < count; i++) {
					items.Add(RegOutput(Registers[0] + i + 1, function));
				}

				dictionary.AddList(items);
			}
		}

		#endregion

		#region Nested type: SetListTo

		public class SetListTo : AbstractInstruction {
			public SetListTo() {
				Mode = EncodedMode.ABx;
			}

			protected override void _execute(LubFunction function) {
				LubErrorHandler.Handle("SetListTo opcode hasn't been implemented yet.", LubSourceError.CodeDecompiler);
			}
		}

		#endregion

		#region Nested type: SetTable

		public class SetTable : AbstractInstruction {
			public SetTable() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				// SETTABLE	A B C	R(A)[RK(B)] := RK(C)
				// There is no output if the table has been loaded (no need to print)

				LubDictionary array = null;

				// First step : find out if the table actually exists
				ILubObject tableName = function.Stack[Registers[0]];

				if (tableName is LubString) {
					// There is no need to instantiate non-global tables
					if (function.Decompiler.GlobalVariables.ContainsKey((LubString) tableName)) {
						array = function.Decompiler.GlobalVariables.GetValue((LubString) tableName) as LubDictionary;
					}
				}
				else {
					array = tableName as LubDictionary;
				}

				if (array != null && function.FunctionLevel == 0) {
					// Even if we're only setting up the table, we have to 
					// be careful about the usage of the settable operand
					// There won't be brackets if the value is a global 
					// variable.
					// There is also a... trick to decide wheter or not
					// brackets should be added : if the table is hashed,
					// then all the variables are globals.
					ILubObject indexer = RegOrK(Registers[1], function);
					string indexerToAd;

					LubValueType indexerAsVt = indexer as LubValueType;

					if (indexerAsVt != null &&
					    ((indexerAsVt.Source == LubSourceType.Global) ||
					     indexerAsVt is LubString && array.Hash > 0) &&
					    !indexer.ToString().StartsWith("/")) {
						indexerToAd = indexer.ToString();
					}
					else {
						indexerToAd = "[" + GetKey(RegOrKOutput(Registers[1], function)) + "]";
					}

					LubString key = new LubString(indexerToAd);
					array.SetKeyValue(key, RegOrKOutput(Registers[2], function));

					if (array.IsAssigned) {
						Append = true;

						StringBuilder builder = new StringBuilder();

						builder.Append(GetKey(tableName));
						array.PrintKey(key, builder, function.FunctionLevel);
						builder.AppendLine();

						LuaCode = builder.ToString();
					}
				}
				else {
					// We print the output
					Append = true;

					StringBuilder builder = new StringBuilder();
					builder.AppendIndent(function.FunctionLevel);

					ILubObject indexer = RegOrK(Registers[1], function);
					string indexerToAd;

					if (indexer is LubValueType &&
					    //((LubValueType)indexer).Source == LubSourceType.Global &&
					    indexer is LubString) {
						indexerToAd = "." + indexer;
					}
					else {
						indexerToAd = "[" + GetKey(RegOrKOutput(Registers[1], function)) + "]";
					}

					builder.Append(GetKey(tableName));
					builder.Append(indexerToAd);
					builder.Append(" = ");
					builder.AppendLine(GetKey(RegOrKOutput(Registers[2], function)).ToString());

					LuaCode = builder.ToString();
				}
			}
		}

		#endregion

		#region Nested type: SetUpVal

		public class SetUpVal : AbstractInstruction {
			public SetUpVal() {
				Mode = EncodedMode.ABC;
			}

			protected override void _execute(LubFunction function) {
				function.UpValues[Registers[1]].Value = GetKey(function.Stack[Registers[0]]);

				Append = true;

				StringBuilder builder = new StringBuilder();
				builder.AppendIndent(function.FunctionLevel);
				builder.Append(function.UpValues[Registers[1]].Key + " = " + function.UpValues[Registers[1]].Value);
				builder.AppendLine();

				LuaCode = builder.ToString();
			}
		}

		#endregion
	}
}