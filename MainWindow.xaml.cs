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
using Microsoft.VisualBasic;

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
        private string _currentLanguage = "en";
        private double _currentGold = 100.0;
        private bool _isHomebrewAllowed = false;
        private bool _isRaceLocked = false;
        private Action _pendingOverlayAction;

        // Ability Scores (PHB Point Buy: Min 8, Max 15)
        private Dictionary<string, int> _abilityScores = new Dictionary<string, int> 
        { 
            { "STR", 8 }, { "DEX", 8 }, { "CON", 8 }, 
            { "INT", 8 }, { "WIS", 8 }, { "CHA", 8 } 
        };

        public MainWindow()
        {
            InitializeComponent();
            LoadAllData();
            
            InventoryDisplay.ItemsSource = _playerInventory;
            MainModeTab.SelectedIndex = 0; 
            UpdateStatsUI(); 
        }

        private void LoadAllData()
        {
            try 
            {
                _allRaces = _xmlLoader.LoadRaces(true); 
                _allLibraryItems = _itemLoader.LoadItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Critical Error: {ex.Message}", "Data Load Failure", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            RefreshUI();
        }

        private void RefreshUI()
        {
            var filteredRaces = _allRaces.Where(r => _isHomebrewAllowed || r.Source == "PHB").ToList();
            RaceList.ItemsSource = filteredRaces;

            ShopList.Items.Clear();
            foreach (var item in _allLibraryItems)
                ShopList.Items.Add($"{item.GetDisplayName(_currentLanguage)} — {item.DefaultCostGp}gp");

            WalletLabel.Text = _currentLanguage == "fr" ? $"{_currentGold} po" : $"{_currentGold} gp";
        }

        // --- NEW CHARACTER WORKFLOW ---

        private void NewCharacter_Click(object sender, RoutedEventArgs e)
        {
            // Reset to default (8s)
            foreach (var key in _abilityScores.Keys.ToList()) _abilityScores[key] = 8;
            UpdateStatsUI();

            ShowOverlay("NEW HERO", 
                "ADVANCED: Start with all 8s (27 points remaining).\nSIMPLE: Start with all 10s (15 points remaining).", 
                () => { 
                    MainModeTab.SelectedIndex = 1; 
                    BuilderTabControl.SelectedIndex = 0; 
                }, 
                false, "", true);
            
            OverlayConfirmBtn.Content = "ADVANCED";
            OverlayAltBtn.Content = "SIMPLE";
        }

        private void SimplePreset_Click(object sender, RoutedEventArgs e)
        {
            // Set all stats to a balanced baseline of 10
            _abilityScores["STR"] = 10;
            _abilityScores["DEX"] = 10;
            _abilityScores["CON"] = 10;
            _abilityScores["INT"] = 10;
            _abilityScores["WIS"] = 10;
            _abilityScores["CHA"] = 10;

            UpdateStatsUI();
            OverlayBlur.Visibility = Visibility.Collapsed;
            
            // Navigate to Stats page so they can spend the remaining 15 points
            MainModeTab.SelectedIndex = 1;
            BuilderTabControl.SelectedIndex = 1;

            ShowOverlay("BALANCED START", "All stats set to 10. You have 15 points left to customize!", null);
        }

        // --- NAVIGATION ---

        private void BackToMenu_Click(object sender, RoutedEventArgs e) => MainModeTab.SelectedIndex = 0;
        private void GoToRace_Click(object sender, RoutedEventArgs e) => BuilderTabControl.SelectedIndex = 0;
        private void GoToStats_Click(object sender, RoutedEventArgs e) => BuilderTabControl.SelectedIndex = 1;

        // --- ABILITY SCORE (POINT BUY) LOGIC ---

        private int GetNextScoreCost(int currentScore)
        {
            // 5e Point Buy: 8-13 (1pt), 14-15 (2pts)
            return (currentScore >= 13) ? 2 : 1;
        }

        private void StatUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string stat)
            {
                int currentScore = _abilityScores[stat];

                if (currentScore >= 15)
                {
                    ShowOverlay("MAX REACHED", "You cannot go above 15 using Point Buy.", null);
                    return;
                }

                int cost = GetNextScoreCost(currentScore);
                int currentPointsSpent = CalculateTotalPointsSpent();

                if (27 - currentPointsSpent >= cost)
                {
                    if (currentScore == 13)
                    {
                        ShowOverlay("HEAVY INVESTMENT", 
                            "Going above 13 costs 2 points. Continue?", 
                            () => ApplyStatChange(stat, 1));
                    }
                    else ApplyStatChange(stat, 1);
                }
                else
                {
                    ShowOverlay("INSUFFICIENT POINTS", "You don't have enough points for this increase.", null);
                }
            }
        }

        private void StatDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string stat)
            {
                if (_abilityScores[stat] > 8) ApplyStatChange(stat, -1);
            }
        }

        private void ApplyStatChange(string stat, int delta)
        {
            _abilityScores[stat] += delta;
            UpdateStatsUI();
        }

        private int CalculateTotalPointsSpent()
        {
            return _abilityScores.Values.Sum(v => {
                // Calculation relative to base 8
                if (v <= 13) return v - 8;
                if (v == 14) return 7; 
                if (v == 15) return 9;
                return 0;
            });
        }

        private void UpdateStatsUI()
        {
            int spent = CalculateTotalPointsSpent();
            int remaining = 27 - spent;

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
            int score = _abilityScores[key];
            valLbl.Text = score.ToString();
            int mod = (int)Math.Floor((score - 10) / 2.0);
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

        // --- RACE SELECTION ---

        public void RaceList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RaceList.SelectedItem != null && !_isRaceLocked) LockRace();
        }

        private void RaceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isRaceLocked || RaceList.SelectedItem is not Race race) return;
            RaceDescription.Text = race.Description;
        }

        private void LockRace()
        {
            if (RaceList.SelectedItem is Race selected)
            {
                _isRaceLocked = true;
                ICollectionView view = CollectionViewSource.GetDefaultView(RaceList.ItemsSource);
                view.Filter = item => ((Race)item).Name == selected.Name;
                UpdateRaceUIState();
            }
        }

        public void UnlockRace_Click(object sender, RoutedEventArgs e)
        {
            _isRaceLocked = false;
            ICollectionView view = CollectionViewSource.GetDefaultView(RaceList.ItemsSource);
            view.Filter = null; 
            UpdateRaceUIState();
            e.Handled = true; 
        }

        private void UpdateRaceUIState()
        {
            NextToStatsBtn.IsEnabled = _isRaceLocked;
            RaceList.UpdateLayout();
            if (RaceList.Items.Count > 0)
            {
                var container = RaceList.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
                if (container != null)
                {
                    var lockCheck = FindChild<TextBlock>(container, "LockCheck");
                    if (lockCheck != null) lockCheck.Visibility = _isRaceLocked ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        // --- SHOP & MISC ---

        private void BuyItem_Click(object sender, RoutedEventArgs e)
        {
            if (ShopList.SelectedIndex == -1) return;
            var item = _allLibraryItems[ShopList.SelectedIndex];
            string displayName = item.GetDisplayName(_currentLanguage);
            
            ShowOverlay($"BUY {displayName}?", $"Cost: {item.DefaultCostGp}gp. Current Gold: {_currentGold}gp.", () => {
                if (_currentGold >= item.DefaultCostGp) {
                    _currentGold -= item.DefaultCostGp;
                    _playerInventory.Add(new InventoryItem { Name = displayName, PricePaid = item.DefaultCostGp, Weight = item.Weight });
                    RefreshUI();
                } else ShowOverlay("NOT ENOUGH GOLD", "Go slay some goblins first!", null);
            });
        }

        private void LanguageToggle_Click(object sender, RoutedEventArgs e) { _currentLanguage = (_currentLanguage == "en") ? "fr" : "en"; RefreshUI(); }
        private void HomebrewToggle_Click(object sender, RoutedEventArgs e) { _isHomebrewAllowed = HomebrewToggle.IsChecked ?? false; RefreshUI(); }
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