using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using DndCharacterBuilder.Models;
using DndCharacterBuilder.Services;
using System.Collections.Generic;

namespace DndCharacterBuilder
{
    // Simple UI Helper for Score Selection
    public class ScoreSelector
    {
        public string Label { get; set; } = "Bonus +1";
        public List<string> Options { get; set; } = new List<string> { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
        public string? SelectedOption { get;set; } 
    }

    public partial class MainWindow : Window
    {
        // Data & Services
        private List<Race> _allRaces = new List<Race>();
        private List<CharacterClass> _allClasses = new List<CharacterClass>();
        private List<Subclass> _allSubclasses = new List<Subclass>();
        private List<Item> _allLibraryItems = new List<Item>();
        private ObservableCollection<InventoryItem> _playerInventory = new ObservableCollection<InventoryItem>();
        
        private XmlLoader _xmlLoader = new XmlLoader();
        private ItemLoader _itemLoader = new ItemLoader();
        private SaveService _saveService = new SaveService();

        // State
        private Character _activeCharacter = new Character();
        private string _currentLanguage = "en";
        // private double _currentGold = 100.0;
        private bool _isHomebrewAllowed = false;
        private bool _isRaceLocked = false;
        private bool _isClassLocked = false;
        private bool _isSubclassLocked = false;
        private Action? _pendingOverlayAction;

        private void InitializeCharacter()
        {
            _activeCharacter = new Character { Name = "" }; // Start empty for input box placeholder
            _activeCharacter.Subclass = null;
            _activeCharacter.BaseStats = new Dictionary<string, int> 
            { 
                { "STR", 8 }, { "DEX", 8 }, { "CON", 8 }, 
                { "INT", 8 }, { "WIS", 8 }, { "CHA", 8 } 
            };
            
            // Allow UI to bind to empty
            if (CharacterNameInput != null) 
            {
                 CharacterNameInput.Text = "";
                 CharacterNamePlaceholder.Visibility = Visibility.Visible;
            }
            _isRaceLocked = false;
            _isClassLocked = false;
            _isSubclassLocked = false;
        }

        public MainWindow()
        {
            InitializeComponent();
            InitializeCharacter();
            LoadAllData();
            
            RefreshVault();

            MainModeTab.SelectedIndex = 0;
            UpdateStatsUI();
        }

        private void RefreshVault()
        {
            var characters = _saveService.LoadAllCharacters();
            CharacterVaultList.ItemsSource = characters;
        }

        private void RefreshVault_Click(object sender, RoutedEventArgs e)
        {
            RefreshVault();
        }

        private void SaveCharacter_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_activeCharacter.Name))
            {
                ShowOverlay("NAME REQUIRED", "Please name your character before saving.", null);
                return;
            }

            // WIP Check
            int pointsSpent = CalculateTotalPointsSpent();
            int budget = GetTotalPointsBudget();
            bool isStatsDone = pointsSpent == budget;
            bool isRaceDone = _activeCharacter.Race != null;
            bool isClassDone = _activeCharacter.Class != null;
            
            _activeCharacter.IsWIP = !(isStatsDone && isRaceDone && isClassDone);

            _saveService.SaveCharacter(_activeCharacter);
            RefreshVault();
            ShowOverlay("SAVED", $"Character '{_activeCharacter.Name}' saved successfully to the Vault.", null);
        }

        // --- CHARACTER NAME INPUT LOGIC ---

        private void CharacterNameInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            CharacterNamePlaceholder.Visibility = string.IsNullOrEmpty(CharacterNameInput.Text) ? Visibility.Visible : Visibility.Collapsed;
            _activeCharacter.Name = CharacterNameInput.Text;
        }

        private void CharacterNameInput_GotFocus(object sender, RoutedEventArgs e)
        {
            CharacterNamePlaceholder.Visibility = Visibility.Collapsed;
        }

        private void CharacterNameInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CharacterNameInput.Text))
            {
                CharacterNamePlaceholder.Visibility = Visibility.Visible;
            }
        }
        
        // --- VAULT INTERACTIONS ---

        private void VaultItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
             // Placeholder for loading logic. 
             // Ideally we would load the XML, hydrate the objects (find race/class by name in loaded lists), set _activeCharacter, and go to builder.
             if (CharacterVaultList.SelectedItem is Character selected)
             {
                 // For now, prompt not fully implemented
                 ShowOverlay("LOAD CHARACTER", $"Loading '{selected.Name}' is not fully implemented yet, but you can view its folder.", null);
             }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string charName)
            {
                string folderPath = _saveService.GetCharacterFolder(charName);
                if (Directory.Exists(folderPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = folderPath,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
            }
        }

        private void LoadAllData()
        {
            try 
            {
                _allRaces = _xmlLoader.LoadRaces(true);
                _allClasses = _xmlLoader.LoadClasses();
                _allSubclasses = _xmlLoader.LoadSubclasses(); 
                _allLibraryItems = _itemLoader.LoadItems();

                ClassList.ItemsSource = _allClasses;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Critical Error: {ex.Message}", "Data Load Failure", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            RefreshUI();
        }

        private void PopulateClasses()
        {
            // Merged into LoadAllData
        }

        private void RefreshUI()
        {
            var filteredRaces = _allRaces.Where(r => _isHomebrewAllowed || r.Source == "FPHb").ToList();
            RaceList.ItemsSource = filteredRaces;
        }

        // --- NEW CHARACTER WORKFLOW ---

        private void NewCharacter_Click(object sender, RoutedEventArgs e)
        {
            InitializeCharacter();
            UpdateStatsUI();

            ShowOverlay("NEW HERO", 
                "ADVANCED: Start with all 8s (27 points remaining).\nSIMPLE: Start with all 10s (15 points remaining).", 
                () => { 
                    MainModeTab.SelectedIndex = 1; 
                    BuilderTabControl.SelectedIndex = 0; // Race Selection is first
                }, 
                false, "", true);

            OverlayConfirmBtn.Content = "ADVANCED";
            OverlayAltBtn.Content = "SIMPLE";
        }

        private void SimplePreset_Click(object sender, RoutedEventArgs e)
        {
            foreach(var key in _activeCharacter.BaseStats.Keys.ToList()) _activeCharacter.BaseStats[key] = 10;

            UpdateStatsUI();
            OverlayBlur.Visibility = Visibility.Collapsed;

            MainModeTab.SelectedIndex = 1;
            BuilderTabControl.SelectedIndex = 0; // Start at Race Selection
        }

        private void LevelSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
             if (LevelSelector.SelectedItem is ComboBoxItem item && int.TryParse(item.Content.ToString(), out int lvl))
             {
                 _activeCharacter.Level = lvl;
                 UpdateLevelUnlocks();
                 UpdateStatsUI();
             }
        }
        
        private void UpdateLevelUnlocks()
        {
            _activeCharacter.Passives.Clear();

            if (_activeCharacter.Race != null)
            {
                // Reset passives to base race passives
                if (_activeCharacter.Race.Passives != null)
                   _activeCharacter.Passives.AddRange(_activeCharacter.Race.Passives);
                
                // Process Race Level Unlocks
                foreach(var unlock in _activeCharacter.Race.Unlocks)
                {
                    if (_activeCharacter.Level >= unlock.Level)
                    {
                        _activeCharacter.Passives.Add($"[Race Lvl {unlock.Level}] {unlock.Description}");
                    }
                }
            }

            if (_activeCharacter.Class != null)
            {
                 // Add base class info or passives
                 if (_activeCharacter.Class.Passives != null)
                   _activeCharacter.Passives.AddRange(_activeCharacter.Class.Passives);

                 // Process Class Level Unlocks
                 foreach(var unlock in _activeCharacter.Class.Unlocks)
                 {
                     if (_activeCharacter.Level >= unlock.Level)
                     {
                         _activeCharacter.Passives.Add($"[Class Lvl {unlock.Level}] {unlock.Description}");
                     }
                 }
            }

            if (_activeCharacter.Subclass != null)
            {
                 // Process Subclass Level Unlocks
                 foreach(var unlock in _activeCharacter.Subclass.Unlocks)
                 {
                     if (_activeCharacter.Level >= unlock.Level)
                     {
                         _activeCharacter.Passives.Add($"[Subclass Lvl {unlock.Level}] {unlock.Description}");
                     }
                 }
            }
        }

        // --- NAVIGATION --- This line is just context for replacement
        private void BackToMenu_Click(object sender, RoutedEventArgs e) => MainModeTab.SelectedIndex = 0;
        private void GoToRace_Click(object sender, RoutedEventArgs e) => BuilderTabControl.SelectedIndex = 0;
        private void GoToClass_Click(object sender, RoutedEventArgs e) => BuilderTabControl.SelectedIndex = 1;
        private void GoToSubclass_Click(object sender, RoutedEventArgs e) => BuilderTabControl.SelectedIndex = 2;
        private void GoToStats_Click(object sender, RoutedEventArgs e) => BuilderTabControl.SelectedIndex = 3;

        // private int _startingLevel = 1; // Removed, using _activeCharacter.Level

        // --- ABILITY SCORE (POINT BUY) LOGIC ---
        
        private int GetTotalPointsBudget()
        {
            // Base budget: 27
            int budget = 27;

            if (_isHomebrewAllowed) 
            {
               budget += 10; 
            }

            int lvl = _activeCharacter.Level;

            if (_activeCharacter.Class != null && _activeCharacter.Class.ASIs != null && _activeCharacter.Class.ASIs.Any())
            {
                // Use class specific ASIs (e.g., Fighters get more)
                foreach(int asiLvl in _activeCharacter.Class.ASIs)
                {
                    if (lvl >= asiLvl) budget += 2;
                }
            }
            else
            {
                // Default PHB ASIs if no class selected
                if (lvl >= 4) budget += 2;
                if (lvl >= 8) budget += 2;
                if (lvl >= 12) budget += 2;
                if (lvl >= 16) budget += 2;
                if (lvl >= 19) budget += 2;
            }

            return budget;
        }

        private int GetNextScoreCost(int currentScore)
        {
            if (currentScore >= 15 && !_isHomebrewAllowed) return 999; // Cap at 15 for standard
            if (currentScore >= 13) return 2;
            return 1;
        }

        private void StatUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string stat)
            {
                int currentScore = _activeCharacter.BaseStats[stat];
                
                // Cap Check
                if (currentScore >= 15 && !_isHomebrewAllowed)
                {
                    ShowOverlay("MAX REACHED", "You cannot go above 15 using Standard Point Buy. Enable Homebrew to bypass.", null);
                    return;
                }
                if (currentScore >= 20) // Absolute hard cap typically 20 in 5e
                {
                     ShowOverlay("HARD CAP", "Attributes cannot exceed 20.", null);
                     return;
                }

                int cost = GetNextScoreCost(currentScore);
                int currentPointsSpent = CalculateTotalPointsSpent();
                int budget = GetTotalPointsBudget();

                if (budget - currentPointsSpent >= cost)
                {
                    if (currentScore == 13 && !_isHomebrewAllowed)
                    {
                        ShowOverlay("HEAVY INVESTMENT", "Going above 13 costs 2 points. Continue?", () => ApplyStatChange(stat, 1));
                    }
                    else ApplyStatChange(stat, 1);
                }
                else ShowOverlay("INSUFFICIENT POINTS", "You don't have enough points for this increase based on your level/settings.", null);
            }
        }

        private void StatDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string stat)
            {
                if (_activeCharacter.BaseStats[stat] > 8) ApplyStatChange(stat, -1);
            }
        }

        private void ApplyStatChange(string stat, int delta)
        {
             _activeCharacter.BaseStats[stat] += delta;
             UpdateStatsUI();
        }

        private int CalculateTotalPointsSpent()
        {
            return _activeCharacter.BaseStats.Values.Sum(v => {
                if (v <= 15)
                {
                    if (v <= 13) return v - 8;
                    if (v == 14) return 7; 
                    if (v == 15) return 9;
                }
                // Extended cost for homebrew > 15
                if (v > 15) return 9 + (v - 15) * 2; 
                return 0;
            });
        }

        private void UpdateStatsUI()
        {
            if (PointsRemainingLabel == null) return; // Prevent crash during initialization

            int budget = GetTotalPointsBudget();
            int spent = CalculateTotalPointsSpent();
            int remaining = budget - spent;

            PointsRemainingLabel.Text = remaining.ToString();
            PointsRemainingLabel.Foreground = remaining < 0 ? Brushes.Crimson : (SolidColorBrush)FindResource("AccentColor");

            UpdateStatLine("STR", StrValue, StrBonus, StrMod);
            UpdateStatLine("DEX", DexValue, DexBonus, DexMod);
            UpdateStatLine("CON", ConValue, ConBonus, ConMod);
            UpdateStatLine("INT", IntValue, IntBonus, IntMod);
            UpdateStatLine("WIS", WisValue, WisBonus, WisMod);
            UpdateStatLine("CHA", ChaValue, ChaBonus, ChaMod);
        }

        private void UpdateStatLine(string key, TextBlock valLbl, TextBlock bonusLbl, TextBlock modLbl)
        {
            if (valLbl == null || modLbl == null) return;
            
            int baseScore = 0;
            if (_activeCharacter.BaseStats.ContainsKey(key))
                baseScore = _activeCharacter.BaseStats[key];

            int racialBonus = 0;

            if (_activeCharacter.Race != null)
            {
                if (_activeCharacter.Race.AbilityBonuses.ContainsKey(key))
                {
                    racialBonus += _activeCharacter.Race.AbilityBonuses[key];
                }
                
                if (_activeCharacter.Race.SelectedScores != null)
                {
                    racialBonus += _activeCharacter.Race.SelectedScores.Count(s => s == key);
                }
            }

            int totalScore = baseScore + racialBonus;
            valLbl.Text = totalScore.ToString();
            
            // Display Bonus indicator (e.g., +1)
            if (racialBonus > 0)
            {
                bonusLbl.Text = $"+{racialBonus}";
                bonusLbl.Visibility = Visibility.Visible;
                valLbl.Foreground = Brushes.LightGreen; 
            }
            else
            {
                bonusLbl.Visibility = Visibility.Collapsed;
                valLbl.Foreground = Brushes.White;
            }

            // Modifier Calculation
            int mod = (totalScore - 10) / 2;
            if (totalScore < 10 && (totalScore - 10) % 2 != 0) mod--; // Handle floor for negatives correctly with integer division math

            modLbl.Text = (mod >= 0 ? "+" + mod : mod.ToString()) + $" to {key} throws";
        }

        // --- OVERLAY SYSTEM ---

        private void ShowOverlay(string title, string message, Action onConfirm, bool showInput = false, string defaultInput = "", bool showSimple = false)
        {
            OverlayTitle.Text = title;
            OverlayMessage.Text = message;
            OverlayInput.Text = defaultInput;
            OverlayInput.Visibility = showInput ? Visibility.Visible : Visibility.Collapsed;
            OverlayAltBtn.Visibility = showSimple ? Visibility.Visible : Visibility.Collapsed;
            _pendingOverlayAction = onConfirm;
            OverlayBlur.Visibility = Visibility.Visible;
            if (onConfirm == null) 
            {
                OverlayConfirmBtn.Content = "OK";
                OverlayConfirmBtn.Visibility = Visibility.Visible; 
            }
            else 
            {
                OverlayConfirmBtn.Content = "CONFIRM";
            }
        }

        private void ConfirmOverlay_Click(object sender, RoutedEventArgs e)
        {
            _pendingOverlayAction?.Invoke();
            OverlayBlur.Visibility = Visibility.Collapsed;
        }

        private void CloseOverlay_Click(object sender, RoutedEventArgs e) => OverlayBlur.Visibility = Visibility.Collapsed;

        // --- SELECTION LOGIC ---

        private void RaceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RaceList.SelectedItem is Race race)
            {
                // ONLY UPDATE DESCRIPTION (and options)
                RaceDescription.Text = race.Description;

                // Show/Hide Options based on ScoreSelectCount
                if (race.ScoreSelectCount > 0)
                {
                    RaceOptionsPanel.Visibility = Visibility.Visible;

                    // Initialize selection list in Race model if empty
                    if (race.SelectedScores == null) race.SelectedScores = new List<string>();
                    
                    // Create selectors for UI
                    var selectors = new List<ScoreSelector>();
                    for (int i = 0; i < race.ScoreSelectCount; i++)
                    {
                        var s = new ScoreSelector 
                        { 
                            Label = $"Bonus +1 (Choice {i + 1})", 
                            SelectedOption = (race.SelectedScores.Count > i) ? race.SelectedScores[i] : null 
                        };
                        selectors.Add(s);
                    }
                    ScoreSelectorsList.ItemsSource = selectors;
                }
                else
                {
                    RaceOptionsPanel.Visibility = Visibility.Collapsed;
                    ScoreSelectorsList.ItemsSource = null;
                }
            }
        }

        private void ScoreSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (RaceList.SelectedItem is not Race race) return;
            if (sender is not ComboBox changedCb) return;

            // Prevent infinite loop from re-binding
            if (ScoreSelectorsList.Items.IsEmpty) return;
            
            var changedSelector = changedCb.DataContext as ScoreSelector;
            if (changedSelector == null) return;
            
            // Get value
            var selectedValue = changedCb.SelectedItem as string;
            changedSelector.SelectedOption = selectedValue;

            var selectors = ScoreSelectorsList.ItemsSource as List<ScoreSelector>;
             
            if (selectors != null && !string.IsNullOrEmpty(selectedValue))
            {
                // Force Clear Duplicate on Other Selectors
                bool changesMade = false;
                foreach(var s in selectors)
                {
                    if (s != changedSelector && s.SelectedOption == selectedValue)
                    {
                        s.SelectedOption = null; 
                        changesMade = true;
                    }
                }
                
                // If a change was made to *another* selector, we need to refresh the UI
                if (changesMade)
                {
                    // HACK: Re-bind to force UI update since ScoreSelector doesn't implement INotifyPropertyChanged
                    // This is jarring (closes dropdown) but effective for this simple scenario
                    ScoreSelectorsList.ItemsSource = null;
                    ScoreSelectorsList.ItemsSource = selectors;
                    return; // Return early prevents double processing as re-bind triggers selection changed again? 
                            // Actually re-bind sets selected items to null/default, might trigger.
                            // But usually ItemsSource=null doesn't trigger SelectionChanged on the *items* control itself, but on the combos inside? 
                            // The combos are destroyed. New combos created.
                }
            }
            
            // Update Model from current state of selectors
            if (selectors != null)
            {
                race.SelectedScores.Clear();
                foreach(var s in selectors)
                { 
                     if (!string.IsNullOrEmpty(s.SelectedOption)) race.SelectedScores.Add(s.SelectedOption);
                }
            }

            // Update Stats
            if (_activeCharacter.Race == race) UpdateStatsUI();
        }

        public void RaceList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RaceList.SelectedItem is Race race && !_isRaceLocked)
            {
                _activeCharacter.Race = race;
                LockRace();
                UpdateLevelUnlocks();
                UpdateStatsUI();
            }
        }

        private void LockRace()
        {
            _isRaceLocked = true;
            UpdateRaceUIState();
        }

        public void UnlockRace_Click(object sender, RoutedEventArgs e)
        {
            _isRaceLocked = false;
            _activeCharacter.Race = null;
            RaceList.SelectedItem = null;
            UpdateRaceUIState();
            UpdateStatsUI(); // Clear stats
            e.Handled = true; 
        }

        private void UpdateRaceUIState()
        {
             var currentList = RaceList.ItemsSource as IEnumerable<Race>;
             if (currentList != null)
             {
                 foreach(var r in currentList)
                 {
                     r.IsSelected = (_isRaceLocked && _activeCharacter.Race == r);
                     r.IsDisabled = (_isRaceLocked && _activeCharacter.Race != r);
                 }
                 RaceList.Items.Refresh();
             }
             if (RaceSelectionCount != null)
             {
                RaceSelectionCount.Text = _isRaceLocked ? "1/1 Selected" : "0/1 Selected";
                RaceSelectionCount.Foreground = _isRaceLocked ? Brushes.LightGreen : Brushes.Orange;
             }
             
             // Disable combos if race locked
             if (_isRaceLocked)
             {
                 RaceOptionsPanel.IsEnabled = false;
                 RaceOptionsPanel.Opacity = 0.5;
             }
             else
             {
                 RaceOptionsPanel.IsEnabled = true;
                 RaceOptionsPanel.Opacity = 1.0;
             }
        }

        private void ClassList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClassList.SelectedItem is CharacterClass cls)
            {
                // ONLY UPDATE DESCRIPTION
                ClassDescription.Text = cls.Description;
            }
        }

        public void ClassList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ClassList.SelectedItem is CharacterClass cls && !_isClassLocked)
            {
                _activeCharacter.Class = cls;
                LockClass();
                UpdateLevelUnlocks();
                UpdateStatsUI();
            }
        }

        private void LockClass()
        {
            _isClassLocked = true;
            UpdateClassUIState();
            UpdateSubclassOptions();
        }

        public void UnlockClass_Click(object sender, RoutedEventArgs e)
        {
             _isClassLocked = false;
            _activeCharacter.Class = null;
            
            // Reset Subclass
            _activeCharacter.Subclass = null;
            _isSubclassLocked = false;
            SubclassList.ItemsSource = null;
            SubclassSelectionCount.Text = "0/1 Selected";
            SubclassSelectionCount.Foreground = Brushes.Orange;
            UpdateSubclassUIState();

            ClassList.SelectedItem = null;
            UpdateClassUIState();
            UpdateStatsUI(); // Clear stats
            e.Handled = true;
        }

        private void UpdateClassUIState()
        {
             var currentList = ClassList.ItemsSource as IEnumerable<CharacterClass>;
             if (currentList != null)
             {
                 foreach(var c in currentList)
                 {
                     c.IsSelected = (_isClassLocked && _activeCharacter.Class == c);
                     c.IsDisabled = (_isClassLocked && _activeCharacter.Class != c);
                 }
                 ClassList.Items.Refresh();
             }
             if (ClassSelectionCount != null)
             {
                ClassSelectionCount.Text = _isClassLocked ? "1/1 Selected" : "0/1 Selected";
                ClassSelectionCount.Foreground = _isClassLocked ? Brushes.LightGreen : Brushes.Orange;
             }
        }

        private void UpdateSubclassOptions()
        {
            if (_activeCharacter.Class == null) 
            {
                SubclassList.ItemsSource = null;
                return;
            }

            var availableSubclasses = _allSubclasses
                .Where(s => s.ParentClass == _activeCharacter.Class.Name)
                .ToList();
            SubclassList.ItemsSource = availableSubclasses;
        }

        private void SubclassList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SubclassList.SelectedItem is Subclass sub)
            {
                SubclassDescription.Text = sub.Description;
            }
        }

        public void SubclassList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SubclassList.SelectedItem is Subclass sub && !_isSubclassLocked)
            {
                _activeCharacter.Subclass = sub;
                LockSubclass();
                UpdateLevelUnlocks();
                UpdateStatsUI();
            }
        }

        private void LockSubclass()
        {
            _isSubclassLocked = true;
            UpdateSubclassUIState();
        }

        public void UnlockSubclass_Click(object sender, RoutedEventArgs e)
        {
            _isSubclassLocked = false;
            _activeCharacter.Subclass = null;
            SubclassList.SelectedItem = null;
            UpdateSubclassUIState();
            UpdateStatsUI(); 
            e.Handled = true;
        }

        private void UpdateSubclassUIState()
        {
             var currentList = SubclassList.ItemsSource as IEnumerable<Subclass>;
             if (currentList != null)
             {
                 foreach(var s in currentList)
                 {
                     s.IsSelected = (_isSubclassLocked && _activeCharacter.Subclass == s);
                     s.IsDisabled = (_isSubclassLocked && _activeCharacter.Subclass != s);
                 }
                 SubclassList.Items.Refresh();
             }
             if (SubclassSelectionCount != null)
             {
                SubclassSelectionCount.Text = _isSubclassLocked ? "1/1 Selected" : "0/1 Selected";
                SubclassSelectionCount.Foreground = _isSubclassLocked ? Brushes.LightGreen : Brushes.Orange;
             }
        }

        // --- SHOP & MISC ---

        private void BuyItem_Click(object sender, RoutedEventArgs e)
        {
            // ShopList is not present in XAML. Shop buy logic skipped.
        }

        private void LanguageToggle_Click(object sender, RoutedEventArgs e) { _currentLanguage = (_currentLanguage == "en") ? "fr" : "en"; RefreshUI(); }
        private void HomebrewToggle_Click(object sender, RoutedEventArgs e) { _isHomebrewAllowed = HomebrewToggle.IsChecked ?? false; UpdateStatsUI(); RefreshUI(); }
        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private T? FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t && child is FrameworkElement fe && fe.Name == childName) return t;
                var result = FindChild<T>(child, childName);
                if (result != null) return result;
            }
            return null;
        }
    }
}