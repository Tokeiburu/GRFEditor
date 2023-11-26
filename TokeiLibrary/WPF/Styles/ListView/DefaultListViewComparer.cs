using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using Utilities;

namespace TokeiLibrary.WPF.Styles.ListView {
	public class DefaultListViewComparer<T> : ListViewCustomComparer {
		private readonly bool _enableAlphaNumSorting;
		private TypeUtility<T>.MemberGetDelegate<byte[]> _getDelegateByteArray;
		private TypeUtility<T>.MemberGetDelegate<int> _getDelegateInt;
		private TypeUtility<T>.MemberGetDelegate<string> _getDelegateString;
		private TypeUtility<T>.MemberGetDelegate<float> _getDelegateFloat;
		private TypeUtility<T>.MemberGetDelegate<long> _getDelegateLong;
		private AlphanumComparator _alphanumComparer = new AlphanumComparator(StringComparison.OrdinalIgnoreCase);
		protected PropertyInfo _property;
		private int _use;

		public DefaultListViewComparer() {
		}

		public DefaultListViewComparer(bool enableAlphaNumSorting) {
			_enableAlphaNumSorting = enableAlphaNumSorting;
		}

		public override void SetSort(string sortColumn, ListSortDirection dir) {
			_direction = dir;
			_property = typeof(T).GetProperty(sortColumn);

			if (_property.PropertyType == typeof(string)) {
				_use = 0;
				_getDelegateString = TypeUtility<T>.GetMemberGetDelegate<string>(sortColumn);
			}
			else if (_property.PropertyType == typeof(int)) {
				_use = 1;
				_getDelegateInt = TypeUtility<T>.GetMemberGetDelegate<int>(sortColumn);
			}
			else if (_property.PropertyType == typeof(byte[])) {
				_use = 2;
				_getDelegateByteArray = TypeUtility<T>.GetMemberGetDelegate<byte[]>(sortColumn);
			}
			else if (_property.PropertyType == typeof(long)) {
				_use = 3;
				_getDelegateLong = TypeUtility<T>.GetMemberGetDelegate<long>(sortColumn);
			}
			else if (_property.PropertyType == typeof(float)) {
				_use = 4;
				_getDelegateFloat = TypeUtility<T>.GetMemberGetDelegate<float>(sortColumn);
			}
			else {
				throw new Exception("Unsupported comparer.");
			}
		}

		public override int Compare(object x, object y) {
			if (_use == 0) {
				string valx = _getDelegateString((T)x);
				string valy = _getDelegateString((T)y);

				if (_enableAlphaNumSorting) {
					if (_direction == ListSortDirection.Ascending)
						return _alphanumComparer.Compare(valx, valy);

					return _alphanumComparer.Compare(valy, valx);
				}

				if (_direction == ListSortDirection.Ascending)
					return String.CompareOrdinal(valx, valy);

				return String.CompareOrdinal(valy, valx);
			}

			if (_use == 1) {
				int x1 = _getDelegateInt((T)x);
				int y1 = _getDelegateInt((T)y);

				return _direction == ListSortDirection.Ascending ? (x1 - y1) : (y1 - x1);
			}

			if (_use == 2) {
				byte[] x1 = _getDelegateByteArray((T)x);
				byte[] y1 = _getDelegateByteArray((T)y);

				if (_direction == ListSortDirection.Ascending) {
					return Methods.ByteArrayCompareToInt(x1, y1);
				}

				if (x1 == null && y1 == null)
					return 0;
				if (x1 == null)
					return 1;
				if (y1 == null)
					return -1;

				return Methods.ByteArrayCompareToInt(y1, x1);
			}

			if (_use == 3) {
				long x1 = (_getDelegateLong((T)x));
				long y1 = (_getDelegateLong((T)y));

				return (int) (_direction == ListSortDirection.Ascending ? (x1 - y1) : (y1 - x1));
			}

			if (_use == 4) {
				int x1 = (int) (_getDelegateFloat((T)x) * 1000);
				int y1 = (int) (_getDelegateFloat((T)y) * 1000);

				return _direction == ListSortDirection.Ascending ? (x1 - y1) : (y1 - x1);
			}

			return 0;
		}
	}

	public static class TypeUtility<TObjectType> {
		#region Delegates

		public delegate MemberType MemberGetDelegate<MemberType>(TObjectType obj);
		public delegate MemberType MemberGetDelegate2<MemberType>(TObjectType obj, int index);

		#endregion

		private static readonly Dictionary<string, Delegate> _memberGetDelegates = new Dictionary<string, Delegate>();

		public static MemberGetDelegate<TMemberType> GetCachedMemberGetDelegate<TMemberType>(string memberName) {
			if (_memberGetDelegates.ContainsKey(memberName))
				return (MemberGetDelegate<TMemberType>)
					   _memberGetDelegates[memberName];

			MemberGetDelegate<TMemberType> returnValue =
				 GetMemberGetDelegate<TMemberType>(memberName);
			lock (_memberGetDelegates) {
				_memberGetDelegates[memberName] = returnValue;
			}
			return returnValue;
		}

		public static MemberGetDelegate<TMemberType> GetMemberGetDelegate<TMemberType>(string memberName) {
			Type objectType = typeof(TObjectType);

			PropertyInfo pi = objectType.GetProperty(memberName);
			FieldInfo fi = objectType.GetField(memberName);
			if (pi != null) {
				// Member is a Property...

				MethodInfo mi = pi.GetGetMethod();
				if (mi != null) {
					// NOTE:  As reader J. Dunlap pointed out...
					//  Calling a property's get accessor is faster/cleaner using
					//  Delegate.CreateDelegate rather than Reflection.Emit 
					return (MemberGetDelegate<TMemberType>)
						Delegate.CreateDelegate(typeof(
							  MemberGetDelegate<TMemberType>), mi);
				}
				else
					throw new Exception(String.Format(
						"Property: '{0}' of Type: '{1}' does" +
						" not have a Public Get accessor",
						memberName, objectType.Name));
			}
			else if (fi != null) {
				// Member is a Field...

				DynamicMethod dm = new DynamicMethod("Get" + memberName,
					typeof(TMemberType), new Type[] { objectType }, objectType);
				ILGenerator il = dm.GetILGenerator();
				// Load the instance of the object (argument 0) onto the stack
				il.Emit(OpCodes.Ldarg_0);
				// Load the value of the object's field (fi) onto the stack
				il.Emit(OpCodes.Ldfld, fi);
				// return the value on the top of the stack
				il.Emit(OpCodes.Ret);

				return (MemberGetDelegate<TMemberType>)
					dm.CreateDelegate(typeof(MemberGetDelegate<TMemberType>));
			}
			else
				throw new Exception(String.Format(
					"Member: '{0}' is not a Public Property or Field of Type: '{1}'",
					memberName, objectType.Name));
		}
	}
}
