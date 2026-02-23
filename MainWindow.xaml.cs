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

        // Ability Scores State (PHB Starting values)
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
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
            RefreshUI();
        }

        private void RefreshUI()
        {
            // Bind the list to the filtered collection
            var filteredRaces = _allRaces.Where(r => _isHomebrewAllowed || r.Source == "PHB").ToList();
            RaceList.ItemsSource = filteredRaces;

            ShopList.Items.Clear();
            foreach (var item in _allLibraryItems)
                ShopList.Items.Add($"{item.GetDisplayName(_currentLanguage)} ({item.DefaultCostGp} gp)");

            WalletLabel.Text = _currentLanguage == "fr" ? $"Or: {_currentGold} po" : $"Gold: {_currentGold} gp";
        }

        // --- NAVIGATION LOGIC ---
        private void GoToRace_Click(object sender, RoutedEventArgs e) => BuilderTabControl.SelectedIndex = 0;
        private void GoToStats_Click(object sender, RoutedEventArgs e) => BuilderTabControl.SelectedIndex = 1;

        private void HomebrewToggle_Click(object sender, RoutedEventArgs e)
        {
            _isHomebrewAllowed = HomebrewToggle.IsChecked ?? false;
            RefreshUI();
        }

        private void LanguageToggle_Click(object sender, RoutedEventArgs e)
        {
            _currentLanguage = (_currentLanguage == "en") ? "fr" : "en";
            RefreshUI();
        }

        // --- CREATOR LOGIC ---

        // FIX: Added MouseDoubleClick to support the XAML event
        public void RaceList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RaceList.SelectedItem != null && !_isRaceLocked)
            {
                LockRace();
            }
        }

        private void RaceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isRaceLocked || RaceList.SelectedItem == null) return;

            if (RaceList.SelectedItem is Race race)
            {
                RaceDescription.Text = race.Description;

            }
        }

        private void ConfirmRace_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRaceLocked) LockRace();
        }

        // FIX: Added UnlockRace_Click to support the XAML event in the DataTemplate
        public void UnlockRace_Click(object sender, RoutedEventArgs e)
        {
            _isRaceLocked = false;
            
            // Clear Filter
            ICollectionView view = CollectionViewSource.GetDefaultView(RaceList.ItemsSource);
            view.Filter = null;

            UpdateRaceUIState();
            e.Handled = true; // Prevent the click from selecting the item again
        }

        private void LockRace()
        {
            if (RaceList.SelectedItem is Race selected)
            {
                _isRaceLocked = true;

                // Apply UI filter to show only selected race
                ICollectionView view = CollectionViewSource.GetDefaultView(RaceList.ItemsSource);
                view.Filter = item => ((Race)item).Name == selected.Name;

                UpdateRaceUIState();
            }
        }

        private void UpdateRaceUIState()
        {
            NextToStatsBtn.IsEnabled = _isRaceLocked;
            
            
            // Refresh the ListBox to apply Template Triggers if any, 
            // but primarily we use the visual tree search to find the Check/Button
            RaceList.UpdateLayout();

            if (RaceList.Items.Count > 0)
            {
                var container = RaceList.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
                if (container != null)
                {
                    var lockCheck = FindChild<TextBlock>(container, "LockCheck");
                    var unlockBtn = FindChild<Button>(container, "SmallUnlockBtn");

                    if (lockCheck != null) lockCheck.Visibility = _isRaceLocked ? Visibility.Visible : Visibility.Collapsed;
                    if (unlockBtn != null) unlockBtn.Visibility = _isRaceLocked ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        // Helper to find elements inside the DataTemplate
        private T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t && child is FrameworkElement fe && fe.Name == childName) return t;
                var result = FindChild<T>(child, childName);
                if (result != null) return result;
            }
            return null;
        }

        // --- ABILITY SCORE LOGIC ---
        private void StatUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string stat)
            {
                if (_abilityScores.ContainsKey(stat) && _abilityScores[stat] < 15) 
                { 
                    _abilityScores[stat]++; 
                    UpdateStatsUI(); 
                }
            }
        }

        private void StatDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string stat)
            {
                if (_abilityScores.ContainsKey(stat) && _abilityScores[stat] > 8) 
                { 
                    _abilityScores[stat]--; 
                    UpdateStatsUI(); 
                }
            }
        }

        private void UpdateStatsUI()
        {
            // Point Buy Cost: 8-13 = 1pt each, 14-15 = 2pts each
            int totalSpent = _abilityScores.Values.Sum(v => v <= 13 ? v - 8 : (v == 14 ? 7 : 9));
            int remaining = 27 - totalSpent;
            
            PointsRemainingLabel.Text = $"Points Remaining: {remaining}";
            PointsRemainingLabel.Foreground = remaining < 0 ? Brushes.Red : Brushes.White;

            StrValue.Text = _abilityScores["STR"].ToString();
            StrMod.Text = GetModStr(_abilityScores["STR"]);
            DexValue.Text = _abilityScores["DEX"].ToString();
            DexMod.Text = GetModStr(_abilityScores["DEX"]);
            ConValue.Text = _abilityScores["CON"].ToString();
            ConMod.Text = GetModStr(_abilityScores["CON"]);
            IntValue.Text = _abilityScores["INT"].ToString();
            IntMod.Text = GetModStr(_abilityScores["INT"]);
            WisValue.Text = _abilityScores["WIS"].ToString();
            WisMod.Text = GetModStr(_abilityScores["WIS"]);
            ChaValue.Text = _abilityScores["CHA"].ToString();
            ChaMod.Text = GetModStr(_abilityScores["CHA"]);
        }

        private string GetModStr(int score)
        {
            int mod = (int)Math.Floor((score - 10) / 2.0);
            return mod >= 0 ? $"+{mod}" : mod.ToString();
        }

        // --- SHOP LOGIC ---
        private void BuyItem_Click(object sender, RoutedEventArgs e)
        {
            if (ShopList.SelectedIndex == -1) return;
            var item = _allLibraryItems[ShopList.SelectedIndex];
            
            string input = Interaction.InputBox("Enter price paid (gp):", "Merchant Transaction", item.DefaultCostGp.ToString());
            
            if (double.TryParse(input, out double price))
            {
                if (_currentGold >= price)
                {
                    _currentGold -= price;
                    _playerInventory.Add(new InventoryItem { 
                        Name = item.GetDisplayName(_currentLanguage), 
                        PricePaid = price,
                        Weight = item.Weight 
                    });
                    RefreshUI();
                }
                else
                {
                    MessageBox.Show("Not enough gold!");
                }
            }
        }

        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) { /* Optional filtering logic */ }
    }
}