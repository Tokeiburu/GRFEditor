using System.Text;

namespace GRF.FileFormats.LubFormat.Types {
	public abstract class LubValueType : ILubObject {
		private LubSourceType _source = LubSourceType.Constant;

		public LubSourceType Source {
			get { return _source; }
			set { _source = value; }
		}

		protected int? Length { get; set; }

		#region ILubObject Members

		public abstract void Print(StringBuilder builder, int level);

		public abstract int GetLength();

		#endregion
	}
}