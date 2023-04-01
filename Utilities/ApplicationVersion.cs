using System;

namespace Utilities {
	public class ApplicationVersion {
		protected bool Equals(ApplicationVersion other) {
			return Equals(_ids, other._ids) && Major == other.Major && Minor == other.Minor && Revision == other.Revision && SubRevision == other.SubRevision;
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((ApplicationVersion) obj);
		}

		public override int GetHashCode() {
			unchecked {
				int hashCode = (_ids != null ? _ids.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ Major;
				hashCode = (hashCode * 397) ^ Minor;
				hashCode = (hashCode * 397) ^ Revision;
				hashCode = (hashCode * 397) ^ SubRevision;
				return hashCode;
			}
		}

		public int Major { get; set; }
		public int Minor { get; set; }
		public int Revision { get; set; }
		public int SubRevision { get; set; }

		private readonly int[] _ids = new int[4];

		public int this[int index] {
			get { return _ids[index]; }
		}

		public ApplicationVersion(string version) {
			string[] values = version.Split('.');

			if (values.Length >= 1)
				Major = Int32.Parse(values[0]);

			if (values.Length >= 2)
				Minor = Int32.Parse(values[1]);

			if (values.Length >= 3)
				Revision = Int32.Parse(values[2]);

			if (values.Length >= 4)
				SubRevision = Int32.Parse(values[3]);

			_ids[0] = Major;
			_ids[1] = Minor;
			_ids[2] = Revision;
			_ids[3] = SubRevision;
		}

		public static bool operator ==(ApplicationVersion version1, ApplicationVersion version2) {
			if (ReferenceEquals(version1, null) && ReferenceEquals(version2, null))
				return true;

			if (ReferenceEquals(version1, null) || ReferenceEquals(version2, null))
				return false;

			for (int i = 0; i < 4; i++) {
				if (version1._ids[i] == version2._ids[i]) {
					continue;
				}

				return false;
			}

			return true;
		}

		public static bool operator !=(ApplicationVersion version1, ApplicationVersion version2) {
			return !(version1 == version2);
		}

		public static bool operator >(ApplicationVersion version1, ApplicationVersion version2) {
			for (int i = 0; i < 4; i++) {
				if (version1._ids[i] > version2._ids[i]) {
					return true;
				}

				if (version1._ids[i] == version2._ids[i]) {
					continue;
				}

				break;
			}


			return false;
		}

		public static bool operator <(ApplicationVersion version1, ApplicationVersion version2) {
			return !(version1 > version2);
		}

		public override string ToString() {
			if (_ids[3] == 0) {
				return String.Format("{0}.{1}.{2}", _ids[0], _ids[1], _ids[2]);
			}

			return String.Format("{0}.{1}.{2}.{3}", _ids[0], _ids[1], _ids[2], _ids[3]);
		}
	}
}
