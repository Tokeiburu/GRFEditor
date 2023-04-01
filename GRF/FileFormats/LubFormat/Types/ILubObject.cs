using System.Text;

namespace GRF.FileFormats.LubFormat.Types {
	public interface ILubObject {
		void Print(StringBuilder builder, int level);
		int GetLength();
	}
}