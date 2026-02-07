using GRF.Threading;
using Xunit;

namespace GRF.Tests {
	public class GrfThreadExtractTests {
		[Fact]
		public void IsLuaBytecode_ReturnsTrueForMagicHeader() {
			byte[] data = { 0x1b, 0x4c, 0x75, 0x61, 0x00 };
			Assert.True(GrfThreadExtract.IsLuaBytecode(data));
		}

		[Fact]
		public void IsLuaBytecode_ReturnsFalseForNonMagicHeader() {
			byte[] data = { 0x00, 0x4c, 0x75, 0x61 };
			Assert.False(GrfThreadExtract.IsLuaBytecode(data));
		}
	}
}
