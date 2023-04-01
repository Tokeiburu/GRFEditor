using System.Collections.Generic;

namespace GRF.FileFormats.LubFormat {
	public class BlockDelimiter {
		public string Label { get; set; }
		public int BlockStart { get; set; }
		public int BlockElseIndex { get; set; }
		public int BlockEnd { get; set; }

		public static List<BlockDelimiter> GetIfElseBlocks(List<string> lines) {
			List<BlockDelimiter> code = new List<BlockDelimiter>();

			for (int i = 0; i < lines.Count; i++) {
				if (LineHelper.IsIf(lines[i])) {
					BlockDelimiter block = new BlockDelimiter();
					block.BlockStart = i;

					int ifIndent = LineHelper.GetIndent(lines[i]);
					int indexElse = LineHelper.FindNextEndsWith(lines, i + 1, ifIndent, "else");
					int indexEnd = LineHelper.FindNextEndsWith(lines, i + 1, ifIndent, "end");

					if (indexElse < 0 || indexEnd < 0)
						continue;

					block.BlockEnd = indexEnd;
					block.BlockElseIndex = indexElse;
					code.Add(block);
				}
			}

			return code;
		}

		public static List<BlockDelimiter> GetIfOrIfElseBlocks(List<string> lines) {
			List<BlockDelimiter> code = new List<BlockDelimiter>();

			for (int i = 0; i < lines.Count; i++) {
				if (LineHelper.IsIf(lines[i])) {
					BlockDelimiter block = new BlockDelimiter();
					block.BlockStart = i;

					int ifIndent = LineHelper.GetIndent(lines[i]);
					int indexElse = LineHelper.FindNextEndsWith(lines, i + 1, ifIndent, "else");
					int indexEnd = LineHelper.FindNextEndsWith(lines, i + 1, ifIndent, "end");

					if (indexElse > indexEnd)
						indexElse = -1;

					block.BlockEnd = indexEnd;
					block.BlockElseIndex = indexElse;
					code.Add(block);
				}
			}

			return code;
		}

		public bool FullyIfContains(BlockDelimiter concernedBlock) {
			return BlockStart + 1 == concernedBlock.BlockStart && concernedBlock.BlockEnd == BlockElseIndex - 1;
		}

		public bool Contains(int line) {
			return BlockStart <= line && line <= BlockEnd;
		}

		public static BlockDelimiter GetConcernedBlock(int line, List<BlockDelimiter> blocks) {
			BlockDelimiter closest = null;
			BlockDelimiter current;

			for (int i = 0; i < blocks.Count; i++) {
				current = blocks[i];

				if (current.BlockStart <= line) {
					closest = current;
				}
				else
					break;
			}

			return closest;
		}

		public static BlockDelimiter GetParentBlock(BlockDelimiter concernedBlock, List<BlockDelimiter> blocks) {
			return GetConcernedBlock(concernedBlock.BlockStart - 1, blocks);
		}
	}
}