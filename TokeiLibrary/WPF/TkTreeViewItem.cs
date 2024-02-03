using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ErrorManager;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Services;
using Utilities.Extension;

namespace TokeiLibrary.WPF {
	public class TkTreeViewItem : TreeViewItem {
		public static DependencyProperty IsDragEnterProperty = DependencyProperty.Register("IsDragEnter", typeof(bool), typeof(TkTreeViewItem), new PropertyMetadata(false));

		public bool IsDragEnter {
			get { return (bool)GetValue(IsDragEnterProperty); }
			set { SetValue(IsDragEnterProperty, value); }
		}

		public static DependencyProperty IsEditModeProperty = DependencyProperty.Register("IsEditMode", typeof(bool), typeof(TkTreeViewItem), new PropertyMetadata(false));

		public bool IsEditMode {
			get { return (bool)GetValue(IsEditModeProperty); }
			set { SetValue(IsEditModeProperty, value); }
		}

		public static DependencyProperty IsUnableItemProperty = DependencyProperty.Register("IsUnableItem", typeof(bool), typeof(TkTreeViewItem), new PropertyMetadata(false));

		public bool IsUnableItem {
			get { return (bool)GetValue(IsUnableItemProperty); }
			set { SetValue(IsUnableItemProperty, value); }
		}

		public static DependencyProperty CanBeDroppedProperty = DependencyProperty.Register("CanBeDropped", typeof(bool), typeof(TkTreeViewItem), new PropertyMetadata(true));

		public bool CanBeDropped {
			get { return (bool)GetValue(CanBeDroppedProperty); }
			set { SetValue(CanBeDroppedProperty, value); }
		}

		public bool CanExpand {
			get { return Items.Count > 0; }
		}

		public static DependencyProperty HeaderTextProperty = DependencyProperty.Register("HeaderText", typeof(string), typeof(TkTreeViewItem), new PropertyMetadata(default(string), HeaderTextPropertyChanged));

		private static void HeaderTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var ttv = d as TkTreeViewItem;

			if (ttv != null) {
				if (Configuration.TranslateTreeView) {
					var translation = TkTreeViewItem._translations[EncodingService.ConvertStringToKorean(ttv.HeaderText)];

					if (translation != null) {
						translation = "  " + translation;
					}

					ttv.TranslatedText = translation;
				}
			}
		}

		public string HeaderText {
			get { return (string)GetValue(HeaderTextProperty); }
			set { SetValue(HeaderTextProperty, value); }
		}

		public static DependencyProperty TranslatedTextProperty = DependencyProperty.Register("TranslatedText", typeof(string), typeof(TkTreeViewItem), new PropertyMetadata(default(string)));

		public string TranslatedText {
			get { return (string)GetValue(TranslatedTextProperty); }
			set { SetValue(TranslatedTextProperty, value); }
		}

		private TkView _internalParent;
		private bool _isSelected;

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
						_parent.SelectedItems.Add(this, _parent);
					}
					else {
						_parent.SelectedItems.Remove(this);
					}

					base.IsSelected = value;
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}
		}

		public TkTreeViewItem(TkView parent, bool setStyle = true) {
			_internalParent = parent;
			this.AllowDrop = true;

			if (setStyle) {
				Style = (Style)this.TryFindResource("TkTreeViewItemStyle");
			}
		}

		public void Reset() {
			IsDragEnter = false;
			IsUnableItem = false;
		}

		public void DragEnterItem() {
			IsDragEnter = true;
		}

		public void UnableItem() {
			IsUnableItem = true;
		}

		public bool CanBeDragged {
			get { return (bool)GetValue(CanBeDraggedProperty); }
			set { SetValue(CanBeDraggedProperty, value); }
		}

		public static DependencyProperty CanBeDraggedProperty = DependencyProperty.Register("CanBeDragged", typeof(bool), typeof(TkTreeViewItem), new PropertyMetadata(true));

		public bool UseCheckBox {
			get { return (bool)GetValue(UseCheckBoxProperty); }
			set { SetValue(UseCheckBoxProperty, value); }
		}

		public static DependencyProperty UseCheckBoxProperty = DependencyProperty.Register("UseCheckBox", typeof(bool), typeof(TkTreeViewItem), new PropertyMetadata(false));

		public bool IsNotUseCheckBox {
			get { return !UseCheckBox; }
		}

		public bool CheckBoxHeaderIsEnabled { get; set; }

		public bool? IsChecked {
			get { return (bool?)GetValue(IsCheckedProperty); }
			set { SetValue(IsCheckedProperty, value); }
		}

		public string CurrentPath { get; set; }
		public string OldHeaderText { get; set; }

		public static DependencyProperty IsCheckedProperty = DependencyProperty.Register("IsChecked", typeof(bool?), typeof(TkTreeViewItem), new PropertyMetadata(false, IsCheckedPropertyChangedCallback));

		private static void IsCheckedPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var ttv = d as TkTreeViewItem;

			if (ttv != null) {
				if (ttv.IsChecked == true)
					ttv.OnChecked(new RoutedEventArgs());
				if (ttv.IsChecked == false)
					ttv.OnUnchecked(new RoutedEventArgs());
				if (ttv.IsChecked == null)
					ttv.OnIndeterminate(new RoutedEventArgs());
			}
		}

		public event RoutedEventHandler Indeterminate;

		protected virtual void OnIndeterminate(RoutedEventArgs e) {
			RoutedEventHandler handler = Indeterminate;
			if (handler != null) handler(this, e);
		}

		public event RoutedEventHandler Checked;

		protected virtual void OnChecked(RoutedEventArgs e) {
			RoutedEventHandler handler = Checked;
			if (handler != null) handler(this, e);
		}

		public event RoutedEventHandler Unchecked;

		protected virtual void OnUnchecked(RoutedEventArgs e) {
			RoutedEventHandler handler = Unchecked;
			if (handler != null) handler(this, e);
		}

		#region Translations
		internal static readonly TkDictionary<string, string> _translations = new TkDictionary<string, string> {
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
			{"로브", "Garments"},
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
			{"가드", "Royal Guard"},
			{"페이욘", "Payon"},
			{"팔라딘", "Paladin"},
			{"사막도시", "Desert Towns"},
			{"기린의날개", "The Wings of the Kirin"},
			{"아버지사랑날개2012", "Loving father Wings 2012"},
			{"슈퍼노비스", "Super Novice"},
			{"인던01", "Inside Dungeon 01"},
			{"타나토스", "Thanatos"},
			//{"아인브로크", "Ain Pembroke"},
			{"마법사", "Mage"},
			{"프론테라", "Prontera"},
			{"연금술사", "Alchemist"},
			{"크리에이터", "Creator"},
			{"방패", "Shields"},
			{"용암동굴", "Magna Dungeon"},
			{"상인", "Merchant"},
			{"인던02", "Inside Dungeon 02"},
			{"제네릭", "Genetic"},
			{"휘겔", "Hugel"},
			{"일본", "Japan"},
			{"헌터", "Hunter"},
			{"이팩트", "Effects"},
			{"스나이퍼", "Sniper"},
			{"중국", "China"},
			{"해변마을", "Seaside town"},
			{"거북이섬", "Turtle Island"},
			{"무희", "Dancer"},
			{"던전", "Dungeons"},
			{"세이지", "Sage"},
			{"프로페서", "Professor"},
			{"소서러", "Sorcerer"},
			{"프론테라내부", "Inside Prontera"},
			{"건너", "Gunslinger"},
			{"성직자", "Acolyte"},
			{"용병", "Mercenary"},
			{"대만", "Taiwan"},
			{"운영자", "Game Master"},
			{"몽크", "Monk"},
			{"길로틴크로스", "Guillotine Cross"},
			{"챔피온", "Champion"},
			{"슈라", "Sura"},
			{"전장", "Battleground"},
			{"크리스마스마을", "Christmas Village"},
			{"페이욘내부", "Inside Payon"},
			{"마도기어", "Magic Gear/Mado"},
			{"닌자", "Ninja"},
			{"피아멧트의리본", "Dark Wings"},
			{"구페코크루세이더", "Crusader (peco)"},
			{"지하감옥", "Dungeon"},
			{"토르화산", "Thor Volcano"},
			{"알데바란", "Aldebaran"},
			{"바드", "Bard"},
			{"권성", "Star Gladiator"},
			{"모로코내부", "Inside Morroc"},
			{"프리스트", "Priest"},
			{"하이프리", "High Priest"},
			{"아크비숍", "Arch Bishop"},
			{"니플헤임", "Niflheim"},
			{"흑마법사방", "Warlock room"},
			{"워프대기실내부", "Inside waiting room"},
			{"알베르타", "Alberta"},
			{"몸통", "Bodies"},
			{"쉐도우체이서", "Shadow Chaser"},
			{"집시마을", "Gypsy village"},
			{"위저드", "Wizard"},
			{"하이위저드", "High Wizard"},
			{"워록", "Warlock"},
			{"게펜내부", "Inside Geffen"},
			{"히나마쯔리", "Doll's Festival"},
			{"궁수", "Archer"},
			{"어비스", "Abyss"},
			{"글래지하수로", "Geulrae into groundwater"},
			{"레인져늑대", "Ranger (warg)"},
			{"레인져", "Ranger"},
			{"태권소년", "Taekwon"},
			{"소울링커", "Soul Linker"},
			{"소울리퍼", "Soul Reaper"},
			{"성제", "Star Emperor"},
			{"워터", "Water"},
			{"산타", "Santa"},
			{"동굴마을", "Cave Town"},
			{"알베르타내부", "Inside Alberta"},
			{"지하묘지", "Catacomb"},
			{"어세신", "Assassin"},
			{"어쌔신크로스", "Assassin Cross"},
			{"제철공", "Blacksmith"},
			{"화이트스미스", "Whitesmith"},
			{"페코페코_기사", "Knight (peco)"},
			{"로그", "Rogue"},
			{"기사", "Knight"},
			{"머리", "Head"},
			{"몸", "Body"},
			{"클라운", "Clown"},
			{"미케닉", "Mechanic"},
			{"민스트럴", "Minstrel"},
			{"스토커", "Stalker"},
			{"집시", "Gyspsy"},
			{"로드나이트", "Lord Knight"},
			{"룬나이트", "Rune Knight"},
			{"원더러", "Wanderer"},
			{"머리통", "Heads"},
			{"도람족", "Doram"},
		};
		#endregion

		public override string ToString() {
			return HeaderText + "; Number of items : " + Items.Count;
		}

		private Dictionary<string, object> _loadedElements;

		public bool Get<T>(string name, out T value) {
			if (_loadedElements == null)
				_loadedElements = new Dictionary<string, object>();

			if (_loadedElements.ContainsKey(name)) {
				value = (T)_loadedElements[name];
				return false;
			}

			var cp = WpfUtilities.FindChild<ContentPresenter>(this);
			value = (T)this.HeaderTemplate.FindName(name, cp);
			_loadedElements[name] = value;
			return true;
		}
	}
}
