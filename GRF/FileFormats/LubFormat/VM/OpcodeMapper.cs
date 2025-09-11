using System;
using System.Collections.Generic;
using Utilities;

namespace GRF.FileFormats.LubFormat.VM {
	public static class OpcodeMapper {
		static OpcodeMapper() {
			List<Func<OpCodes.AbstractInstruction>> instructions;
			instructions = new List<Func<OpCodes.AbstractInstruction>>();
			instructions.Add(() => new OpCodes.Move());
			instructions.Add(() => new OpCodes.LoadK());
			instructions.Add(() => new OpCodes.LoadBool());
			instructions.Add(() => new OpCodes.LoadNil());
			instructions.Add(() => new OpCodes.GetUpVal());
			instructions.Add(() => new OpCodes.GetGlobal());
			instructions.Add(() => new OpCodes.GetTable());
			instructions.Add(() => new OpCodes.SetGlobal());
			instructions.Add(() => new OpCodes.SetUpVal());
			instructions.Add(() => new OpCodes.SetTable());
			instructions.Add(() => new OpCodes.NewTable());
			instructions.Add(() => new OpCodes.Self());
			instructions.Add(() => new OpCodes.Add());
			instructions.Add(() => new OpCodes.Sub());
			instructions.Add(() => new OpCodes.Mul());
			instructions.Add(() => new OpCodes.Div());
			instructions.Add(() => new OpCodes.Pow());
			instructions.Add(() => new OpCodes.UnaryMinus());
			instructions.Add(() => new OpCodes.Not());
			instructions.Add(() => new OpCodes.Concat());
			instructions.Add(() => new OpCodes.Jmp());
			instructions.Add(() => new OpCodes.Eq());
			instructions.Add(() => new OpCodes.Lt());
			instructions.Add(() => new OpCodes.Le());
			instructions.Add(() => new OpCodes.TestSet());
			instructions.Add(() => new OpCodes.Call());
			instructions.Add(() => new OpCodes.TailCall());
			instructions.Add(() => new OpCodes.Return());
			instructions.Add(() => new OpCodes.ForLoop50());
			instructions.Add(() => new OpCodes.TableForLoop50());
			instructions.Add(() => new OpCodes.TableForPrep());
			instructions.Add(() => new OpCodes.SetList50());
			instructions.Add(() => new OpCodes.SetListTo());
			instructions.Add(() => new OpCodes.Close());
			instructions.Add(() => new OpCodes.Closure());
			InstructionSets["0x500"] = instructions;

			instructions = new List<Func<OpCodes.AbstractInstruction>>();
			instructions.Add(() => new OpCodes.Move());
			instructions.Add(() => new OpCodes.LoadK());
			instructions.Add(() => new OpCodes.LoadBool());
			instructions.Add(() => new OpCodes.LoadNil());
			instructions.Add(() => new OpCodes.GetUpVal());
			instructions.Add(() => new OpCodes.GetGlobal());
			instructions.Add(() => new OpCodes.GetTable());
			instructions.Add(() => new OpCodes.SetGlobal());
			instructions.Add(() => new OpCodes.SetUpVal());
			instructions.Add(() => new OpCodes.SetTable());
			instructions.Add(() => new OpCodes.NewTable());
			instructions.Add(() => new OpCodes.Self());
			instructions.Add(() => new OpCodes.Add());
			instructions.Add(() => new OpCodes.Sub());
			instructions.Add(() => new OpCodes.Mul());
			instructions.Add(() => new OpCodes.Div());
			instructions.Add(() => new OpCodes.Mod());
			instructions.Add(() => new OpCodes.Pow());
			instructions.Add(() => new OpCodes.UnaryMinus());
			instructions.Add(() => new OpCodes.Not());
			instructions.Add(() => new OpCodes.Len());
			instructions.Add(() => new OpCodes.Concat());
			instructions.Add(() => new OpCodes.Jmp());
			instructions.Add(() => new OpCodes.Eq());
			instructions.Add(() => new OpCodes.Lt());
			instructions.Add(() => new OpCodes.Le());
			instructions.Add(() => new OpCodes.Test());
			instructions.Add(() => new OpCodes.TestSet());
			instructions.Add(() => new OpCodes.Call());
			instructions.Add(() => new OpCodes.TailCall());
			instructions.Add(() => new OpCodes.Return());
			instructions.Add(() => new OpCodes.ForLoop51());
			instructions.Add(() => new OpCodes.ForPrep());
			instructions.Add(() => new OpCodes.TableForLoop51());
			instructions.Add(() => new OpCodes.SetList51());
			instructions.Add(() => new OpCodes.Close());
			instructions.Add(() => new OpCodes.Closure());
			instructions.Add(() => new OpCodes.VarArg());
			InstructionSets["0x501"] = instructions;
		}

		public static readonly Dictionary<string, List<Func<OpCodes.AbstractInstruction>>> InstructionSets = new Dictionary<string, List<Func<OpCodes.AbstractInstruction>>>();

		public static OpCodes.AbstractInstruction GetInstruction(byte[] codeByteData, List<Func<OpCodes.AbstractInstruction>> instructionSet, Lub decompiler) {
			int codeInt = BitConverter.ToInt32(codeByteData, 0);

			int opcode = codeInt & 0x3f;

			if (opcode >= instructionSet.Count) {
				LubErrorHandler.Handle("Failed to retrieve the opcode : " + opcode + ".", LubSourceError.LubReader);
			}

			OpCodes.AbstractInstruction instrution = instructionSet[opcode]();
			instrution.Load(codeInt, decompiler);
			return instrution;
		}
	}
}