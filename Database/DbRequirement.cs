using System;

namespace Database {
	[Flags]
	public enum ServerType {
		RAthena = 1 << 0,
		Hercules = 1 << 1,
		Unknown = 1 << 2,
		Both = RAthena | Hercules,
	}

	[Flags]
	public enum RenewalType {
		PreRenewal = 1 << 0,
		Renewal = 1 << 1,
		Both = PreRenewal | Renewal,
	}

	public class DbRequirement {
		private ServerType _type = ServerType.Both;
		private RenewalType _renewal = RenewalType.Both;
		private int _attributeCount = -1;

		public int AttributeCount {
			get { return _attributeCount; }
			set { _attributeCount = value; }
		}

		public ServerType Server {
			get { return _type; }
			set { _type = value; }
		}

		public RenewalType Renewal {
			get { return _renewal; }
			set { _renewal = value; }
		}
	}
}
