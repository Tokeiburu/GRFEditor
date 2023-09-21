using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ErrorManager;
using Utilities;
using Utilities.Services;

namespace TokeiLibrary.WPF {
	public class TkTreeViewItem : TreeViewItem {
		public bool CanBeDragged {
			get { return (bool)GetValue(CanBeDraggedProperty); }
			set { SetValue(CanBeDraggedProperty, value); }
		}

		public static DependencyProperty CanBeDraggedProperty = DependencyProperty.Register("CanBeDragged", typeof(bool), typeof(TkTreeViewItem), new PropertyMetadata(true));

		public bool CanBeDropped {
			get { return (bool)GetValue(CanBeDroppedProperty); }
			set { SetValue(CanBeDroppedProperty, value); }
		}

		public static DependencyProperty CanBeDroppedProperty = DependencyProperty.Register("CanBeDropped", typeof(bool), typeof(TkTreeViewItem), new PropertyMetadata(true));

		private CheckBox _checkBox;
		protected string _closedPathIcon = "folderClosed.png";
		private static readonly Brush _defaultBackgroundBrush = _bufferBrush(Colors.Transparent);
		private static readonly Brush _defaultBorderBrush = _bufferBrush(Colors.Transparent);
		private static readonly Brush _dragEnterBackgroundBrush = _bufferBrush(Color.FromArgb(255, 225, 255, 219), Color.FromArgb(255, 144, 255, 137), 90);
		private static readonly Brush _dragEnterBorderBrush = _bufferBrush(Color.FromArgb(255, 86, 197, 86));
		protected Image _image;
		private TkView _internalParent;
		private bool _isSelected;
		private static readonly Brush _mouseOverBackgroundBrush = _bufferBrush(Color.FromArgb(255, 250, 251, 253), Color.FromArgb(255, 235, 243, 253), 90);
		private static readonly Brush _mouseOverBorderBrush = _bufferBrush(Color.FromArgb(255, 184, 214, 251));
		protected string _openedPathIcon = "folderOpened.png";
		private static readonly Brush _selectBackgroundBrush = _bufferBrush(Color.FromArgb(255, 233, 243, 255), Color.FromArgb(255, 193, 219, 252), 90);
		private static readonly Brush _selectBorderBrush = _bufferBrush(Color.FromArgb(255, 125, 162, 206));
		private TextBlock _tbDisplayVirtual;
		private Border _tviHeader;
		private Border _tviHeaderBorder;
		private static Brush _unableBackgroundBrush = _bufferBrush(Color.FromArgb(255, 255, 208, 208), Color.FromArgb(255, 247, 96, 96), 90);
		private static Brush _unableBorderBrush = _bufferBrush(Color.FromArgb(255, 188, 18, 18));
		private TextBlock _tbDisplay;

		public TextBlock TbDisply {
			get { return _tbDisplay; }
		}

		private static Brush _bufferBrush(Color color) {
			var brush = new SolidColorBrush(color);
			brush.Freeze();
			return brush;
		}

		private static Brush _bufferBrush(Color color1, Color color2, double angle) {
			var brush = new LinearGradientBrush(color1, color2, angle);
			brush.Freeze();
			return brush;
		}

		public TkTreeViewItem(TkView parent) {
			Loaded += new RoutedEventHandler(_tkTreeViewItem_Loaded);

			_internalParent = parent;
			this.AllowDrop = true;
			
			OnInitialized(null);
		}

		private TkView _parent {
			get { return _internalParent ?? (_internalParent = WpfUtilities.FindDirectParentControl<TkView>(this)); }
		}

		public new bool IsSelected {
			get {
				return _isSelected;
			}
			set {
				_isSelected = value;

				try {
					if (_isSelected) {
						TVIHeaderBrush.Background = SelectBackgroundBrush;
						TVIHeaderBrush.BorderBrush = SelectBorderBrush;
						_tviHeader.Background = _parent.Background;
						_tbDisplay.Foreground = MouseOverTextForeground;

						_parent.SelectedItems.Add(this, _parent);
					}
					else {
						TVIHeaderBrush.Background = DefaultBackgroundBrush;
						TVIHeaderBrush.BorderBrush = DefaultBorderBrush;
						_tviHeader.Background = Brushes.Transparent;
						_tbDisplay.Foreground = DefaultTextForeground;

						_parent.SelectedItems.Remove(this);
					}

					base.IsSelected = value;
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}
		}

		public Image ViewImage {
			get { return _image; }
		}

		public string HeaderText {
			get { return _tbDisplay.Text; }
			set {
				_tbDisplay.Text = value;
				_tbDisplay.Foreground = DefaultTextForeground;

				if (Configuration.TranslateTreeView) {
					var translation = _translations[EncodingService.ConvertStringToKorean(value)];

					if (translation != null) {
						translation = "  " + translation;
					}

					_tbDisplayVirtual.Text = translation;
				}
			}
		}

		private static readonly TkDictionary<string, string> _translations = new TkDictionary<string, string> {
			{"검사", "Swordsman"},
			{"인간족", "Player"},
			{"모로코", "Morroc"},
			{"유저인터페이스", "User interface"},
			{"아이템", "Items - drag"},
			{"남", "Male"},
			{"악세사리", "Accessories"},
			{"필드바닥", "Field grounds"},
			{"유노", "Yuno"},
			{"초보자", "Novice"},
			{"여", "Female"},
			{"기타마을", "Other towns"},
			{"모험가배낭", "Adventurer's Backpack"},
			{"로브", "Robes"},
			{"내부소품", "Interior accessories"},
			{"타락천사의날개", "Wings of fallen angels"},
			{"천사날개", "Angel Wings"},
			{"루드라의날개", "Wings of Rudra"},
			{"행복의날개", "Wings of Happiness"},
			{"기타마을내부", "Other villages inside"},
			{"몬스터", "Monsters"},
			//{"외부소품", "Outside props"},
			{"신페코크루세이더", "Crusader (armored peco)"},
			{"라헬", "Rachel"},
			{"나무잡초꽃", "Trees"},
			{"도둑", "Thief"},
			{"크루세이더", "Crusader"},
			{"페이욘", "Payon"},
			{"사막도시", "Desert Towns"},
			{"기린의날개", "The Wings of the Kirin"},
			{"아버지사랑날개2012", "Loving father Wings 2012"},
			{"슈퍼노비스", "Super novice"},
			{"인던01", "Inside Dungeon 01"},
			{"타나토스", "Thanatos"},
			//{"아인브로크", "Ain Pembroke"},
			{"마법사", "Mage"},
			{"프론테라", "Prontera"},
			{"연금술사", "Alchemist"},
			{"방패", "Shields"},
			{"용암동굴", "Magna Dungeon"},
			{"상인", "Merchant"},
			{"인던02", "Inside Dungeon 02"},
			{"휘겔", "Hugel"},
			{"일본", "Japan"},
			{"헌터", "Hunter"},
			{"이팩트", "Effects"},
			{"중국", "China"},
			{"해변마을", "Seaside town"},
			{"거북이섬", "Turtle Island"},
			{"무희", "Dancer"},
			{"던전", "Dungeons"},
			{"세이지", "Sage"},
			{"프론테라내부", "Inside Prontera"},
			{"건너", "Gunslinger"},
			{"성직자", "Acolyte"},
			{"용병", "Mercenary"},
			{"대만", "Taiwan"},
			{"운영자", "Game Master"},
			{"몽크", "Monk"},
			{"전장", "Battleground"},
			{"크리스마스마을", "Christmas Village"},
			{"페이욘내부", "Inside Payon"},
			{"마도기어", "Magic Gear"},
			{"닌자", "Ninja"},
			{"피아멧트의리본", "Dark Wings"},
			{"구페코크루세이더", "Crusader (peco)"},
			{"지하감옥", "Dungeon"},
			{"토르화산", "Thor Volcano"},
			{"알데바란", "Aldebaran"},
			{"바드", "Bard"},
			{"모로코내부", "Inside Morroc"},
			{"프리스트", "Priest"},
			{"니플헤임", "Niflheim"},
			{"흑마법사방", "Warlock room"},
			{"워프대기실내부", "Inside waiting room"},
			{"알베르타", "Alberta"},
			{"몸통", "Bodies"},
			{"쉐도우체이서", "Shadow Chaser"},
			{"집시마을", "Gypsy village"},
			{"위저드", "Wizard"},
			{"게펜내부", "Inside Geffen"},
			{"히나마쯔리", "Doll's Festival"},
			{"궁수", "Archer"},
			{"어비스", "Abyss"},
			{"글래지하수로", "Geulrae into groundwater"},
			{"레인져늑대", "Ranger (warg)"},
			{"태권소년", "Taekwon"},
			{"워터", "Water"},
			{"동굴마을", "Cave Town"},
			{"알베르타내부", "Inside Alberta"},
			{"지하묘지", "Catacomb"},
			{"어세신", "Assassin"},
			{"제철공", "Blacksmith"},
			{"페코페코_기사", "Knight (peco)"},
			{"로그", "Rogue"},
			{"기사", "Knight"},
			{"머리", "Head"},
			{"몸", "Body"},
			{"머리통", "Heads"},
			{"도람족", "Doram"},
		};

		public TextBlock TextBlock {
			get { return _tbDisplay; }
		}

		public Border TVIHeaderBrush {
			get { return _tviHeaderBorder; }
		}

		public bool UseCheckBox {
			set {
				if (value) {
					_image.Visibility = Visibility.Collapsed;
					_checkBox.Visibility = Visibility.Visible;
				}
				else {
					_image.Visibility = Visibility.Visible;
					_checkBox.Visibility = Visibility.Collapsed;
				}
			}
		}

		public CheckBox CheckBoxHeader {
			get { return _checkBox; }
		}

		public virtual string PathIconClosed {
			get { return _closedPathIcon; }
			set {
				_closedPathIcon = value;
				if (IsLoaded) {
					_image.Source = ApplicationManager.PreloadResourceImage(IsExpanded ? _openedPathIcon : _closedPathIcon);
					_image.Stretch = Stretch.Uniform;
					_image.Height = 16;
				}
			}
		}

		public virtual string PathIconOpened {
			get { return _openedPathIcon; }
			set {
				_openedPathIcon = value;
				if (IsLoaded) {
					_image.Source = ApplicationManager.PreloadResourceImage(IsExpanded ? _openedPathIcon : _closedPathIcon);
					_image.Stretch = Stretch.Uniform;
					_image.Height = 16;
				}
			}
		}

		#region Brushes
		public static DependencyProperty MouseOverBorderBrushProperty = DependencyProperty.Register("MouseOverBorderBrush", typeof(Brush), typeof(TkTreeViewItem), new PropertyMetadata(_mouseOverBorderBrush));
		public static DependencyProperty MouseOverBackgroundBrushProperty = DependencyProperty.Register("MouseOverBackgroundBrush", typeof(Brush), typeof(TkTreeViewItem), new PropertyMetadata(_mouseOverBackgroundBrush));
		public static DependencyProperty DefaultBackgroundBrushProperty = DependencyProperty.Register("DefaultBackgroundBrush", typeof(Brush), typeof(TkTreeViewItem), new PropertyMetadata(_defaultBackgroundBrush));
		public static DependencyProperty DefaultBorderBrushProperty = DependencyProperty.Register("DefaultBorderBrush", typeof(Brush), typeof(TkTreeViewItem), new PropertyMetadata(_defaultBorderBrush));
		public static DependencyProperty SelectBackgroundBrushProperty = DependencyProperty.Register("SelectBackgroundBrush", typeof(Brush), typeof(TkTreeViewItem), new PropertyMetadata(_selectBackgroundBrush));
		public static DependencyProperty SelectBorderBrushProperty = DependencyProperty.Register("SelectBorderBrush", typeof(Brush), typeof(TkTreeViewItem), new PropertyMetadata(_selectBorderBrush));
		public static DependencyProperty DragEnterBackgroundBrushProperty = DependencyProperty.Register("DragEnterBackgroundBrush", typeof(Brush), typeof(TkTreeViewItem), new PropertyMetadata(_dragEnterBackgroundBrush));
		public static DependencyProperty DragEnterBorderBrushProperty = DependencyProperty.Register("DragEnterBorderBrush", typeof(Brush), typeof(TkTreeViewItem), new PropertyMetadata(_dragEnterBorderBrush));
		public static DependencyProperty MouseOverTextForegroundProperty = DependencyProperty.Register("MouseOverTextForeground", typeof(Brush), typeof(TkTreeViewItem), new PropertyMetadata(Brushes.Black, OnMouseOverTextForegroundChanged));
		public static DependencyProperty SelectedTextForegroundProperty = DependencyProperty.Register("SelectedTextForeground", typeof(Brush), typeof(TkTreeViewItem), new PropertyMetadata(Brushes.Black, OnSelectedTextForegroundChanged));
		public static DependencyProperty DefaultTextForegroundProperty = DependencyProperty.Register("DefaultTextForeground", typeof(Brush), typeof(TkTreeViewItem), new PropertyMetadata(Brushes.Black, new PropertyChangedCallback(OnDefaultTextForegroundChanged)));
		public static DependencyProperty UnableBackgroundBrushProperty = DependencyProperty.Register("UnableBackgroundBrush", typeof(Brush), typeof(TkTreeViewItem), new PropertyMetadata(_unableBackgroundBrush));
		public static DependencyProperty UnableBorderBrushProperty = DependencyProperty.Register("UnableBorderBrush", typeof(Brush), typeof(TkTreeViewItem), new PropertyMetadata(_unableBorderBrush));

		public Brush DefaultTextForeground {
			get { return (Brush) GetValue(DefaultTextForegroundProperty); }
			set { SetValue(DefaultTextForegroundProperty, value); }
		}

		private static void OnDefaultTextForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var ttv = d as TkTreeViewItem;

			if (ttv != null) {
				if (!ttv.IsSelected)
					ttv._tbDisplay.Foreground = (Brush) e.NewValue;
			}
		}

		public Brush SelectedTextForeground {
			get { return (Brush)GetValue(SelectedTextForegroundProperty); }
			set { SetValue(SelectedTextForegroundProperty, value); }
		}

		private static void OnSelectedTextForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var ttv = d as TkTreeViewItem;

			if (ttv != null) {
				if (ttv.IsSelected)
					ttv._tbDisplay.Foreground = (Brush)e.NewValue;
			}
		}

		private static void OnMouseOverTextForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var ttv = d as TkTreeViewItem;

			if (ttv != null) {
				if (ttv.IsSelected) {
					ttv._tbDisplay.Foreground = (Brush) e.NewValue;
				}
			}
		}

		public Brush BackgroundColor {
			set {
				if (value is SolidColorBrush) {
					//
				}
				else {
					_tviHeader.Background = value;
				}
			}
		}

		public Brush MouseOverTextForeground {
			get { return (Brush)GetValue(MouseOverTextForegroundProperty); }
			set { SetValue(MouseOverTextForegroundProperty, value); }
		}
		
		public Brush MouseOverBorderBrush {
			get { return (Brush)GetValue(MouseOverBorderBrushProperty); }
			set { SetValue(MouseOverBorderBrushProperty, value); }
		}
		
		public Brush MouseOverBackgroundBrush {
			get { return (Brush)GetValue(MouseOverBackgroundBrushProperty); }
			set { SetValue(MouseOverBackgroundBrushProperty, value); }
		}
		
		public Brush DefaultBackgroundBrush {
			get { return (Brush)GetValue(DefaultBackgroundBrushProperty); }
			set { SetValue(DefaultBackgroundBrushProperty, value); }
		}

		public Brush DefaultBorderBrush {
			get { return (Brush)GetValue(DefaultBorderBrushProperty); }
			set { SetValue(DefaultBorderBrushProperty, value); }
		}
        
		public Brush SelectBackgroundBrush {
			get { return (Brush)GetValue(SelectBackgroundBrushProperty); }
			set { SetValue(SelectBackgroundBrushProperty, value); }
		}
        
		public Brush SelectBorderBrush {
			get { return (Brush)GetValue(SelectBorderBrushProperty); }
			set { SetValue(SelectBorderBrushProperty, value); }
		}
		
		public Brush DragEnterBackgroundBrush {
			get { return (Brush)GetValue(DragEnterBackgroundBrushProperty); }
			set { SetValue(DragEnterBackgroundBrushProperty, value); }
		}
        
		public Brush DragEnterBorderBrush {
			get { return (Brush)GetValue(DragEnterBorderBrushProperty); }
			set { SetValue(DragEnterBorderBrushProperty, value); }
		}
		
		public Brush UnableBackgroundBrush {
			get { return (Brush)GetValue(UnableBackgroundBrushProperty); }
			set { SetValue(UnableBackgroundBrushProperty, value); }
		}
        
		public Brush UnableBorderBrush {
			get { return (Brush)GetValue(UnableBorderBrushProperty); }
			set { SetValue(UnableBorderBrushProperty, value); }
		}

		#endregion

		public override string ToString() {
			return HeaderText + "; Number of items : " + Items.Count;
		}

		protected override void OnInitialized(EventArgs e) {
			_tviHeader = new Border { Background = Brushes.Transparent, Margin = new Thickness(-2, 0, 0, 0) };

			Header = _tviHeader;

			_tviHeaderBorder = new Border { CornerRadius = new CornerRadius(2), BorderThickness = new Thickness(1) };

			_tviHeader.Child = _tviHeaderBorder;

			StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(3, 0, 1, 0) };

			_tviHeaderBorder.Child = panel;

			_checkBox = new CheckBox();
			_checkBox.VerticalAlignment = VerticalAlignment.Center;
			_checkBox.Foreground = Application.Current.Resources["TextForeground"] as Brush;
			_image = new Image();
			_tbDisplay = new TextBlock { Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
			_tbDisplayVirtual = new TextBlock { FontStyle = FontStyles.Italic, Foreground = new SolidColorBrush(Colors.Gray), Margin = new Thickness(0, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };

			panel.Children.Add(_checkBox);
			_checkBox.Visibility = Visibility.Collapsed;
			panel.Children.Add(_image);
			panel.Children.Add(_tbDisplay);
			panel.Children.Add(_tbDisplayVirtual);

			//_tbDisplay.Text = HeaderText;
			_tviHeader.MouseEnter += new MouseEventHandler(_tkTreeViewItem_MouseEnter);
			_tviHeader.MouseLeave += new MouseEventHandler(_tkTreeViewItem_MouseLeave);
			LostFocus += new RoutedEventHandler(_tviHeader_LostFocus);
			_tviHeader.MouseLeftButtonDown += (s, a) => {
				if (a.ClickCount == 2) {
					IsExpanded = !IsExpanded;
				}
			};
			_tviHeader.MouseLeftButtonDown += new MouseButtonEventHandler(_tkTreeViewItem_MouseLeftButtonDown);
			_tviHeader.MouseRightButtonDown += new MouseButtonEventHandler(_tkTreeViewItem_MouseRightButtonDown);
			base.OnInitialized(e);
		}

		private void _tviHeader_LostFocus(object sender, RoutedEventArgs e) {
			TkTreeViewItem item = WpfUtilities.FindParentControl<TkTreeViewItem>(sender as Border);

			if (item != null) {
				if (!item.IsSelected) {
					item.TVIHeaderBrush.Background = DefaultBackgroundBrush;
					item.TVIHeaderBrush.BorderBrush = DefaultBorderBrush;
				}
			}
		}

		private void _tkTreeViewItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
			if (_parent.SelectedItem != this) {
				IsSelected = true;
			}

			e.Handled = true;
		}

		private void _tkTreeViewItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) {
				IsSelected = !IsSelected;
			}
			else {
				if (_parent.SelectedItem != this) {
					IsSelected = true;
					_tbDisplay.Foreground = SelectedTextForeground;
				}
			}

			e.Handled = true;
		}

		public void UnableItem() {
			TVIHeaderBrush.Background = UnableBackgroundBrush;
			TVIHeaderBrush.BorderBrush = UnableBorderBrush;
			_tbDisplay.Foreground = MouseOverTextForeground;
		}

		public void DragEnterItem() {
			TVIHeaderBrush.Background = DragEnterBackgroundBrush;
			TVIHeaderBrush.BorderBrush = DragEnterBorderBrush;
			_tbDisplay.Foreground = MouseOverTextForeground;
		}

		private void _tkTreeViewItem_MouseLeave(object sender, MouseEventArgs e) {
			TkTreeViewItem item = WpfUtilities.FindParentControl<TkTreeViewItem>(sender as Border);

			if (item != null) {
				if (!item.IsSelected) {
					item.TVIHeaderBrush.Background = DefaultBackgroundBrush;
					item.TVIHeaderBrush.BorderBrush = DefaultBorderBrush;
					item._tbDisplay.Foreground = DefaultTextForeground;
				}
			}
		}

		private void _tkTreeViewItem_MouseEnter(object sender, MouseEventArgs e) {
			TkTreeViewItem item = WpfUtilities.FindParentControl<TkTreeViewItem>(sender as Border);

			if (item != null) {
				if (!item.IsSelected) {
					item.TVIHeaderBrush.Background = MouseOverBackgroundBrush;
					item.TVIHeaderBrush.BorderBrush = MouseOverBorderBrush;
					item._tbDisplay.Foreground = MouseOverTextForeground;
				}
			}
		}

		protected virtual void _tkTreeViewItem_Loaded(object sender, RoutedEventArgs e) {
			PathIconClosed = _closedPathIcon;
			PathIconOpened = _openedPathIcon;
		}

		protected override void OnCollapsed(RoutedEventArgs e) {
			_image.Source = ApplicationManager.GetResourceImage(_closedPathIcon);
			base.OnCollapsed(e);
		}
		protected override void OnExpanded(RoutedEventArgs e) {
			_image.Source = ApplicationManager.GetResourceImage(_openedPathIcon);

			if (Items.Count == 0) {
				IsExpanded = false;
				e.Handled = true;
				return;
			}

			base.OnExpanded(e);
		}

		public void Reset() {
			if (_isSelected) {
				TVIHeaderBrush.Background = SelectBackgroundBrush;
				TVIHeaderBrush.BorderBrush = SelectBorderBrush;
				_tviHeader.Background = _parent.Background;
				//_tbDisplay.Foreground = MouseOverTextForeground;
				_tbDisplay.Foreground = SelectedTextForeground;
			}
			else {
				TVIHeaderBrush.Background = DefaultBackgroundBrush;
				TVIHeaderBrush.BorderBrush = DefaultBorderBrush;
				_tviHeader.Background = Brushes.Transparent;
				_tbDisplay.Foreground = DefaultTextForeground;
			}
		}
	}
}
