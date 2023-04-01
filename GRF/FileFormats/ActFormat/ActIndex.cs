namespace GRF.FileFormats.ActFormat {
	public class ActIndex {
		public int ActionIndex { get; set; }
		public int FrameIndex { get; set; }
		public int LayerIndex { get; set; }

		public bool Default {
			get { return true; }
		}

		protected bool Equals(ActIndex other) {
			return LayerIndex == other.LayerIndex && FrameIndex == other.FrameIndex && ActionIndex == other.ActionIndex;
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((ActIndex) obj);
		}

		public override int GetHashCode() {
			unchecked {
				int hashCode = LayerIndex;
				hashCode = (hashCode * 397) ^ FrameIndex;
				hashCode = (hashCode * 397) ^ ActionIndex;
				return hashCode;
			}
		}
	}
}