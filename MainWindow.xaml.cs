using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DndCharacterBuilder.Models;
using DndCharacterBuilder.Services;
using Microsoft.VisualBasic; // Ensure you ran: dotnet add package Microsoft.VisualBasic

namespace DndCharacterBuilder
{
    public partial class MainWindow : Window
    {
        // Data Storage
        private List<Race> _allRaces = new List<Race>();
        private List<Item> _allLibraryItems = new List<Item>();
        private ObservableCollection<InventoryItem> _playerInventory = new ObservableCollection<InventoryItem>();
        
        // Services
        private XmlLoader _xmlLoader = new XmlLoader();
        private ItemLoader _itemLoader = new ItemLoader();
        
        // State
        private string _currentLanguage = "en";
        private double _currentGold = 100.0;
        private bool _isHomebrewAllowed = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadAllData();
            InventoryDisplay.ItemsSource = _playerInventory;
        }

        private void LoadAllData()
        {
            _allRaces = _xmlLoader.LoadRaces(true); // Load all, filter in UI
            _allLibraryItems = _itemLoader.LoadItems();
            RefreshUI();
        }

        private void RefreshUI()
        {
            // Update Race Tab
            RaceList.Items.Clear();
            var filteredRaces = _allRaces.Where(r => _isHomebrewAllowed || r.Source == "PHB");
            foreach (var r in filteredRaces) RaceList.Items.Add($"{r.Name} ({r.Source})");

            // Update Shop Tab
            ShopList.Items.Clear();
            foreach (var item in _allLibraryItems)
            {
                string name = item.GetDisplayName(_currentLanguage);
                string currency = _currentLanguage == "fr" ? "po" : "gp";
                ShopList.Items.Add($"{name} ({item.DefaultCostGp} {currency}) - {item.Weight} lbs");
            }

            // Update Labels
            WalletLabel.Text = _currentLanguage == "fr" ? $"Or: {_currentGold} po" : $"Gold: {_currentGold} gp";
        }

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

        private void BuyItem_Click(object sender, RoutedEventArgs e)
        {
            if (ShopList.SelectedIndex == -1) return;

            var selectedItem = _allLibraryItems[ShopList.SelectedIndex];
            
            // Popup for Custom Price (DM Negotiation)
            string prompt = _currentLanguage == "fr" ? "Prix final (po) :" : "Final Price (gp):";
            string title = _currentLanguage == "fr" ? "Achat" : "Transaction";
            string input = Interaction.InputBox(prompt, title, selectedItem.DefaultCostGp.ToString());

            if (double.TryParse(input, out double finalPrice))
            {
                if (_currentGold >= finalPrice)
                {
                    _currentGold -= finalPrice;
                    
                    _playerInventory.Add(new InventoryItem {
                        Name = selectedItem.GetDisplayName(_currentLanguage),
                        PricePaid = finalPrice,
                        Weight = selectedItem.Weight
                    });

                    RefreshUI();
                }
                else
                {
                    MessageBox.Show(_currentLanguage == "fr" ? "Pas assez d'or !" : "Not enough gold!");
                }
            }
        }

        private void RaceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RaceList.SelectedIndex == -1) return;
            // Find the race by name (simplified for now)
            string selectedName = RaceList.SelectedItem.ToString().Split('(')[0].Trim();
            var race = _allRaces.FirstOrDefault(r => r.Name == selectedName);
            RaceDescription.Text = race?.Description ?? "";
        }
    }
}