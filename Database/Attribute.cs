using System;
using Utilities.Extension;

namespace Database {
	public class DbAttribute {
		protected bool Equals(DbAttribute other) {
			return Index == other.Index;
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((DbAttribute) obj);
		}

		public override int GetHashCode() {
			return Index;
		}

		private IAccessor _accessor = AccessFunctions.DefaultAccessor;
		private IValueConverter _dataConverter = ValueConverter.DefaultConverter;
		private VisibleState _visibility = VisibleState.Visible;
		private IDataCopy _dataCopy = DataCopyParser.DefaultDataCopyParser;
		private DbRequirement _requirements = new DbRequirement();
		public object AttachedObject { get; set; }
		public bool? IsSearchable { get; set; }
		public object AttachedAttribute { get; set; }
		public bool IsDisplayAttribute { get; set; }
		public bool IsSkippable { get; set; }
		public bool IsEnabled { get; set; }

		public DbRequirement Requirements {
			get { return _requirements; }
			set { _requirements = value; }
		}

		public IAccessor Accessor {
			get { return _accessor; }
			protected set { _accessor = value; }
		}

		public IValueConverter DataConverter {
			get { return _dataConverter; }
			protected set { _dataConverter = value; }
		}

		public IDataCopy DataCopy {
			get { return _dataCopy; }
			set { _dataCopy = value; }
		}

		public DbAttribute(DbAttribute attribute) {
			AttributeName = attribute.AttributeName;
			DataType = attribute.DataType;
			Default = attribute.Default;
			PrimaryKey = attribute.PrimaryKey;
			Index = attribute.Index;
			DisplayName = attribute.DisplayName;
			DataConverter = attribute.DataConverter;
		}

		public DbAttribute(string attributeName, Type domainDefinition, object defaultValue) {
			AttributeName = attributeName;
			DataType = domainDefinition;
			Default = defaultValue;
			DisplayName = attributeName;
		}

		public DbAttribute(string attributeName, Type domainDefinition, object defaultValue, string domainName)
			: this(attributeName, domainDefinition, defaultValue) {
				DisplayName = domainName;
		}

		public Type DataType { get; protected set; }
		public string AttributeName { get; protected set; }
		public string DisplayName { get; protected set; }
		public string Description { get; protected set; }
		public bool PrimaryKey { get; protected set; }
		public int Index { get; set; }
		public VisibleState Visibility {
			get { return _visibility; }
			set { _visibility = value; }
		}

		public IBinding Binding { get; set; }
		public object Default { get; set; }

		public string GetQueryName() {
			return AttributeList.GetQueryName(DisplayName);
		}
	}

	public interface IBinding {
		Tuple Tuple { get; set; }
		DbAttribute AttachedAttribute { get; set; }
	}

	public class PrimaryAttribute : DbAttribute {
		public PrimaryAttribute(string attributeName, Type domainDefinition, object defaultValue, string domainName = null)
			: base(attributeName, domainDefinition, defaultValue, domainName) {
			PrimaryKey = true;
		}
	}

	[Flags]
	public enum VisibleState {
		Visible = 1 << 0,
		Hidden = 1 << 1,
		ForceShow = 1 << 2,
		VisibleAndForceShow = Visible | ForceShow,
	}
}
