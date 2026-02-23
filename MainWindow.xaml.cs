using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using DndCharacterBuilder.Models;
using DndCharacterBuilder.Services;

namespace DndCharacterBuilder
{
    public partial class MainWindow : Window
    {
        // Data & Services
        private List<Race> _allRaces = new List<Race>();
        private List<Item> _allLibraryItems = new List<Item>();
        private ObservableCollection<InventoryItem> _playerInventory = new ObservableCollection<InventoryItem>();
        private XmlLoader _xmlLoader = new XmlLoader();
        private ItemLoader _itemLoader = new ItemLoader();

        // State
        private Character _activeCharacter = new Character();
        private string _currentLanguage = "en";
        private double _currentGold = 100.0;
        private bool _isHomebrewAllowed = false;
        private bool _isRaceLocked = false;
        private Action _pendingOverlayAction;

        private void InitializeCharacter()
        {
            _activeCharacter = new Character();
            _activeCharacter.BaseStats = new Dictionary<string, int> 
            { 
                { "STR", 8 }, { "DEX", 8 }, { "CON", 8 }, 
                { "INT", 8 }, { "WIS", 8 }, { "CHA", 8 } 
            };
        }

        public MainWindow()
        {
            InitializeComponent();
            InitializeCharacter();
            LoadAllData();
            
            MainModeTab.SelectedIndex = 0;
            UpdateStatsUI();
        }

        private void LoadAllData()
        {
            try 
            {
                _allRaces = _xmlLoader.LoadRaces(true); 
                _allLibraryItems = _itemLoader.LoadItems();

                // Simulation de chargement de classes (Peut être remplacé par un ClassLoader plus tard)
                PopulateClasses();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Critical Error: {ex.Message}", "Data Load Failure", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            RefreshUI();
        }

        private void PopulateClasses()
        {
            var classes = _xmlLoader.LoadClasses();
            ClassList.ItemsSource = classes;
        }

        private void RefreshUI()
        {
            var filteredRaces = _allRaces.Where(r => _isHomebrewAllowed || r.Source == "PHB").ToList();
            RaceList.ItemsSource = filteredRaces;

            // ShopList and WalletLabel are not present in XAML. Shop UI refresh skipped.
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
        }

        // --- NAVIGATION --- This line is just context for replacement
        private void BackToMenu_Click(object sender, RoutedEventArgs e) => MainModeTab.SelectedIndex = 0;
        private void GoToRace_Click(object sender, RoutedEventArgs e) => BuilderTabControl.SelectedIndex = 0;
        private void GoToClass_Click(object sender, RoutedEventArgs e) => BuilderTabControl.SelectedIndex = 1;
        private void GoToStats_Click(object sender, RoutedEventArgs e) => BuilderTabControl.SelectedIndex = 2;

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

            UpdateStatLine("STR", StrValue, StrMod);
            UpdateStatLine("DEX", DexValue, DexMod);
            UpdateStatLine("CON", ConValue, ConMod);
            UpdateStatLine("INT", IntValue, IntMod);
            UpdateStatLine("WIS", WisValue, WisMod);
            UpdateStatLine("CHA", ChaValue, ChaMod);
        }

        private void UpdateStatLine(string key, TextBlock valLbl, TextBlock modLbl)
        {
            if (valLbl == null || modLbl == null) return;
            
            int baseScore = _activeCharacter.BaseStats[key];
            int racialBonus = 0;
            if (_activeCharacter.Race != null && _activeCharacter.Race.AbilityBonuses.ContainsKey(key))
            {
                racialBonus = _activeCharacter.Race.AbilityBonuses[key];
            }
            // Handle "Any" bonuses logic if needed? For now, standard racial stats.
            
            int totalScore = baseScore + racialBonus;
            
            valLbl.Text = totalScore.ToString();
            // Show bonus visually
            if (racialBonus > 0) valLbl.Foreground = Brushes.LightGreen;
            else valLbl.Foreground = Brushes.White;

            int mod = (int)Math.Floor((totalScore - 10) / 2.0);
            modLbl.Text = mod >= 0 ? $"+{mod}" : mod.ToString();
        }

        // --- OVERLAY SYSTEM ---

        private void ShowOverlay(string title, string message, Action onConfirm, bool showInput = false, string defaultInput = "", bool showSimple = false)
        {
            OverlayTitle.Text = title;
            OverlayMessage.Text = message;
            OverlayInput.Visibility = showInput ? Visibility.Visible : Visibility.Collapsed;
            OverlayAltBtn.Visibility = showSimple ? Visibility.Visible : Visibility.Collapsed;
            _pendingOverlayAction = onConfirm;
            OverlayBlur.Visibility = Visibility.Visible;
            OverlayConfirmBtn.Content = onConfirm == null ? "OK" : "CONFIRM";
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
            if (RaceList.SelectedItem is not Race race) return; // Allow selection even if locked for viewing, but assume locked means logic handled elsewhere or list disabled
            
            _activeCharacter.Race = race;
            RaceDescription.Text = race.Description;
            UpdateLevelUnlocks();
            UpdateStatsUI();
        }

        private void ClassList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClassList.SelectedItem is CharacterClass selected)
            {
                _activeCharacter.Class = selected;
                ClassDescription.Text = selected.Description;
                UpdateLevelUnlocks();
                // If the class changes, the character might have different features/stats affecting points (like ASI from Fighter 6)
                // ASIs from class are mostly choices, but let's refresh UI anyway.
                UpdateStatsUI(); 
            }
        }

        public void RaceList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RaceList.SelectedItem != null && !_isRaceLocked) LockRace();
        }

        private void LockRace()
        {
            if (RaceList.SelectedItem is Race selected)
            {
                _isRaceLocked = true;
                RaceList.IsEnabled = false; // Grey out the list to lock it
                UpdateRaceUIState();
            }
        }

        public void UnlockRace_Click(object sender, RoutedEventArgs e)
        {
            _isRaceLocked = false;
            RaceList.IsEnabled = true; // Unlock the list
            UpdateRaceUIState();
            e.Handled = true; 
        }

        private void UpdateRaceUIState()
        {
            if (RaceList.SelectedItem != null)
            {
                 // We don't really need to toggle a checkmark if the whole list is disabled to indicate lock.
                 // But if we want to show a lock icon on the selected item, we'd need to find its container.
                 // For now, let's just rely on the list being disabled as visual feedback.
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