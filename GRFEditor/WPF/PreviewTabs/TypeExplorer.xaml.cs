using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ErrorManager;
using GRF.FileFormats;
using GRF.FileFormats.RsmFormat;
using GRF.FileFormats.RswFormat.RswObjects;
using GRF.Graphics;
using GRF.Image;
using GrfToWpfBridge.TreeViewManager;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for TypeExplorer.xaml
	/// </summary>
	public partial class TypeExplorer : UserControl {
		#region Delegates

		public delegate bool CancelLoadingDelegate();

		#endregion

		private static readonly List<Type> _expandableTypes = new List<Type>();
		private static readonly List<Type> _nonExpandableTypes = new List<Type>();

		static TypeExplorer() {
			_expandableTypes.Add(typeof (String));
			_nonExpandableTypes.Add(typeof (float));
			_nonExpandableTypes.Add(typeof (Boolean));
			_nonExpandableTypes.Add(typeof (byte));
			_nonExpandableTypes.Add(typeof (UInt32));
			_nonExpandableTypes.Add(typeof (UInt16));
			_nonExpandableTypes.Add(typeof (TkVector2));
			_nonExpandableTypes.Add(typeof (TkVector3));
			_nonExpandableTypes.Add(typeof (TkVector4));
			_nonExpandableTypes.Add(typeof (TextureVertex));
			_nonExpandableTypes.Add(typeof (Face));
			_nonExpandableTypes.Add(typeof (Mesh));
			_nonExpandableTypes.Add(typeof (GrfImage));
			_nonExpandableTypes.Add(typeof (FileHeader));
			_nonExpandableTypes.Add(typeof (RswObject));
			_nonExpandableTypes.Add(typeof (byte[]));
		}

		public TypeExplorer() {
			InitializeComponent();
		}

		public void LoadObject(object item, CancelLoadingDelegate cancelLoadingCallback, int expandLevel = 1) {
			_view.Dispatch(p => p.Items.Clear());

			if (expandLevel < 0)
				expandLevel = 0;

			_view.BeginDispatch(delegate {
				var node = _addNode(null, item, null, false, null, cancelLoadingCallback);
				if (node == null)
					return;
				_view.Items.Add(node);
				_expand(_view.Items[0] as TypeTreeViewItem, expandLevel, cancelLoadingCallback);
			});
		}

		private void _expand(TypeTreeViewItem parent, int expandLevel, CancelLoadingDelegate cancelLoadingCallback) {
			try {
				if (expandLevel < 0)
					return;

				if (cancelLoadingCallback())
					return;

				parent.BeginDispatch(new Action(delegate {
					if (parent.DontAutomaticallyExpand)
						return;

					if (parent.Items.Count > 0)
						parent.IsExpanded = true;
				}));

				foreach (TypeTreeViewItem item in (IEnumerable<TypeTreeViewItem>) parent.Dispatcher.Invoke(new Func<List<TypeTreeViewItem>>(() => parent.Items.Cast<TypeTreeViewItem>().ToList()))) {
					if (cancelLoadingCallback())
						return;

					_expand(item, expandLevel - 1, cancelLoadingCallback);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private TypeTreeViewItem _addNode(TypeTreeViewItem parentNode, object item, PropertyInfo propertyInfo, bool expanded, string forcePropertyName, CancelLoadingDelegate cancelLoadingCallback) {
			if (cancelLoadingCallback())
				return null;

			try {
				if (parentNode == null) {
					Type type = item.GetType();

					TypeTreeViewItem tvi = new TypeTreeViewItem(_view, TypeTreeViewItemClass.ClassType);
					tvi.HeaderText = _cleanPropertyName(Path.GetExtension(type.ToString()).Substring(1));

					List<TypeTreeViewItem> itemsToAdd = new List<TypeTreeViewItem>();

					foreach (PropertyInfo p in type.GetProperties().Where(p => p.CanRead)) {
						TypeTreeViewItem tviToAdd = _addNode(tvi, item, p, false, null, cancelLoadingCallback);
						if (tviToAdd != null)
							itemsToAdd.Add(tviToAdd);
					}

					foreach (TypeTreeViewItem tviToAdd in itemsToAdd.Where(p => p.HasBeenLoaded == null)) {
						tvi.Items.Add(tviToAdd);
					}

					foreach (TypeTreeViewItem tviToAdd in itemsToAdd.Where(p => p.HasBeenLoaded == false)) {
						tvi.Items.Add(tviToAdd);
					}

					return tvi;
				}
				else if (propertyInfo == null && forcePropertyName == null) {
					Type type = item.GetType();

					List<TypeTreeViewItem> itemsToAdd = new List<TypeTreeViewItem>();

					foreach (PropertyInfo p in type.GetProperties().Where(p => p.CanRead)) {
						if (p.Name == "Item")
							continue;

						TypeTreeViewItem tviToAdd = _addNode(parentNode, item, p, false, null, cancelLoadingCallback);
						if (tviToAdd != null)
							itemsToAdd.Add(tviToAdd);
					}

					foreach (TypeTreeViewItem tviToAdd in itemsToAdd.Where(p => p.HasBeenLoaded == null)) {
						parentNode.Items.Add(tviToAdd);
					}

					foreach (TypeTreeViewItem tviToAdd in itemsToAdd.Where(p => p.HasBeenLoaded == false)) {
						parentNode.Items.Add(tviToAdd);
					}

					return null;
				}
				else if (expanded) {
					return null;
				}
				else {
					object obj;

					if (propertyInfo == null) {
						obj = item;
					}
					else {
						obj = propertyInfo.GetValue(item, null);
					}

					string propertyName = forcePropertyName ?? propertyInfo.Name;

					propertyName = _cleanPropertyName(propertyName);

					TypeTreeViewItem tvi = new TypeTreeViewItem(_view, TypeTreeViewItemClass.MemberType);

					if (propertyName == "EncryptionKey" || propertyName == "Encryption key") {
						return null;
					}

					if (obj == null) {
						tvi.HeaderText = propertyName + " | " + "[Null value]";
					}
					else {
						if (propertyName == "Key" && obj is String) {
							tvi.HeaderText = propertyName + " | " + "\"" + BitConverter.ToString(EncodingService.DisplayEncoding.GetBytes((String) obj)) + "\"";
						}
						else if (propertyName == "EncryptionKey") {
							// Ignored
						}
						else if ((propertyName == "NewCompressedData" || propertyName == "New compressed data") && obj is IList) {
							tvi.HeaderText = propertyName + " | " + "[Ignored property]";
						}
						else if (propertyName == "DataImage" || propertyName == "Data image") {
							tvi.HeaderText = propertyName + " | " + "[Ignored property]";
						}
						else if (obj is Image || obj is ImageSource) {
							tvi.HeaderText = propertyName + " | " + "[Ignored property]";
						}
						else if (propertyName == "Files" && obj is IList) {
							tvi.HeaderText = propertyName + " | " + "[Ignored property]";
						}
						else if (obj is UInt32 || obj is Int32 || obj is Int16 || obj is UInt16 || obj is long || obj is ulong) {
							tvi.HeaderText = propertyName + " | " + obj;
						}
						else if (obj is byte) {
							tvi.HeaderText = propertyName + " | " + ((Byte)obj).ToString(CultureInfo.InvariantCulture);
						}
						else if (obj is Double) {
							tvi.HeaderText = propertyName + " | " + ((Double)obj).ToString(CultureInfo.InvariantCulture);
						}
						else if (obj is Single) {
							tvi.HeaderText = propertyName + " | " + ((Single)obj).ToString(CultureInfo.InvariantCulture);
						}
						else if (obj is String) {
							tvi.HeaderText = propertyName + " | " + "\"" + ((String)obj).ToString(CultureInfo.InvariantCulture) + "\"";
						}
						else if (obj is GrfColor) {
							tvi.HeaderText = propertyName + " | " + "{" + obj + "}";
						}
						else if (obj is Boolean) {
							tvi.HeaderText = propertyName + " | " + "{" + ((Boolean)obj).ToString(CultureInfo.InvariantCulture) + "}";
						}
						else if (obj is Stream) {
							tvi.HeaderText = propertyName + " | " + "{Stream}";
						}
						else if (obj is Enum) {
							tvi.HeaderText = propertyName + " | " + "{" + obj + "}";
						}
						else {
							if (obj is IList && ((IList) obj).Count > 0) {
								tvi.HeaderText = propertyName + " | " + "{Count = " + ((IList)obj).Count.ToString(CultureInfo.InvariantCulture) + "}";
								tvi.HasBeenLoaded = false;
								tvi.Items.Add(new TypeTreeViewItem(_view, TypeTreeViewItemClass.TooManyType) { HeaderText = "..." });
								tvi.ObjectType = TypeTreeViewItemClass.ListType;
							
								Type type = obj.GetType();
								if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (List<>)) {
									Type itemType = type.GetGenericArguments()[0];
							
									if (_nonExpandableTypes.Contains(itemType) || _nonExpandableTypes.Contains(itemType.BaseType)) {
										tvi.DontAutomaticallyExpand = true;
									}
								}
							
								if (obj is Array) {
									tvi.DontAutomaticallyExpand = true;
								}
							
								if (((IList) obj).Count > 200) {
									//tvi.Expanded += delegate {
									//if (!_isExpanding)
									//ErrorHandler.HandleException("Too many items to load... (limit set to 200).");
									//};
								}
								else {
									tvi.Expanded += delegate {
										if (tvi.HasBeenLoaded == false) {
											tvi.Items.Clear();
										
											for (int index = 0; index < ((IList) obj).Count; index++) {
												object oj = ((IList) obj)[index];
										
												if (cancelLoadingCallback())
													return;
										
												TypeTreeViewItem tviItem = _addNode(tvi, oj, null, false, propertyName + "[" + index + "]", cancelLoadingCallback);
												if (_nonExpandableTypes.Contains(oj.GetType()) || _nonExpandableTypes.Contains(oj.GetType().BaseType)) {
												}
										
												if (tviItem != null)
													tvi.Items.Add(tviItem);
											}
											tvi.HasBeenLoaded = true;
										}
									};
								}
							}
							else {
								if (obj is IList) {
									tvi.ObjectType = TypeTreeViewItemClass.ListType;
									tvi.HeaderText = propertyName + " | " + ((IList)obj).Count.ToString(CultureInfo.InvariantCulture);
								}
								else {
									tvi.HeaderText = propertyName + " | " + "{" + obj + "}";
									tvi.HasBeenLoaded = false;
									tvi.ObjectType = TypeTreeViewItemClass.ClassType;
									tvi.Items.Add(new TypeTreeViewItem(_view, TypeTreeViewItemClass.TooManyType) { HeaderText = "..." });
							
									if (_nonExpandableTypes.Contains(obj.GetType()) || _nonExpandableTypes.Contains(obj.GetType().BaseType)) {
										tvi.DontAutomaticallyExpand = true;
									}
							
									TypeTreeViewItem tviClosure = tvi;
									Object objClosure = obj;
							
									tvi.Expanded += delegate {
										if (tviClosure.HasBeenLoaded == false) {
											tviClosure.Items.Clear();
											_addNode(tviClosure, objClosure, null, false, null, cancelLoadingCallback);
											tviClosure.HasBeenLoaded = true;
										}
									};
								}
							}
						}
					}

					return tvi;
				}
			}
			catch {
				return null;
			}
		}

		private string _cleanPropertyName(string propertyName) {
			List<string> segments = new List<string>();

			int currentPosition = 0;
			int wordOldPosition;

			while (currentPosition < propertyName.Length) {
				if (_isCap(currentPosition, propertyName)) {
					wordOldPosition = currentPosition;
					currentPosition++;

					if (currentPosition < propertyName.Length) {
						//GRFFileEntry  <--- retrieves 'GRF'
						if (_isCap(currentPosition, propertyName)) {
							while (currentPosition < propertyName.Length && _isCap(currentPosition, propertyName)) {
								currentPosition++;
							}

							if (currentPosition != propertyName.Length)
								currentPosition--;

							segments.Add(_subString(wordOldPosition, currentPosition, propertyName));
						}
						else {
							//FileEntry  <--- retrieves 'File'
							while (currentPosition < propertyName.Length && !_isCap(currentPosition, propertyName)) {
								currentPosition++;
							}

							segments.Add(_subString(wordOldPosition, currentPosition, propertyName));
						}
					}
					else {
						segments.Add(_subString(wordOldPosition, currentPosition, propertyName));
					}
				}
				else {
					currentPosition++;
				}
			}

			if (segments.Count == 0) {
				segments.Add(propertyName);
			}

			var s = string.Join(" ", segments.Select(p => p.Length > 1 ? p.ToLower() : p).ToArray());
			if (s.Length > 0) {
				s = char.ToUpper(s[0]) + s.Substring(1);
			}
			return s;
		}

		private string _subString(int indexStart, int indexEnd, string value) {
			return value.Substring(indexStart, indexEnd - indexStart);
		}

		private bool _isCap(int index, string value) {
			return value[index] >= 'A' && value[index] <= 'Z';
		}
	}
}