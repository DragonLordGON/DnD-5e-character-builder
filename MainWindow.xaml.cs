using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using DndCharacterBuilder.Models;
using DndCharacterBuilder.Services;
using System.Collections.Generic;
using DndCharacterBuilder.Controls;

namespace DndCharacterBuilder
{
    // Simple UI Helper for Score Selection
    public class ScoreSelector
    {
        public string Label { get; set; } = "Bonus +1";
        public List<string> Options { get; set; } = new List<string> { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
        public string? SelectedOption { get;set; } 
    }

    public partial class MainWindow : Avalonia.Controls.Window
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
                 CharacterNamePlaceholder.IsVisible = true;
            }

            _isRaceLocked = false;
            _isClassLocked = false;
            _isSubclassLocked = false;

            // Clear Descriptions
            if (RaceDescription != null) RaceDescription.Text = "Please select a race to see details.";
            if (ClassDescription != null) ClassDescription.Text = "Please select a class to see details.";
            if (SubclassDescription != null) SubclassDescription.Text = "Please select a subclass to see details.";

            if (RaceOptionsPanel != null) RaceOptionsPanel.IsVisible = false;

            // Reset UI States
            UpdateRaceUIState();
            UpdateClassUIState();
            UpdateSubclassOptions();
            UpdateStatsUI();
            UpdatePortraitUI();
        }

        private void RollDice(int sides, int result)
        {
            var dice = new DiceControl();
            DiceArena.Children.Add(dice);
            
            // Start position: middle of the screen
            Canvas.SetLeft(dice, (this.Bounds.Width / 2) - (64 / 2));
            Canvas.SetTop(dice, (this.Bounds.Height / 2) - (64 / 2));
            
            dice.Roll(result, sides, DiceArena.Bounds);
        }

        private void TestRoll_Click(object? sender, RoutedEventArgs e)
        {
            // Simulate a d20 roll
            var random = new Random();
            int result = random.Next(1, 21);
            RollDice(20, result);
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
            CharacterNamePlaceholder.IsVisible = string.IsNullOrEmpty(CharacterNameInput.Text);
            _activeCharacter.Name = CharacterNameInput.Text ?? string.Empty;
        }

        private void CharacterNameInput_GotFocus(object sender, RoutedEventArgs e)
        {
            CharacterNamePlaceholder.IsVisible = false;
        }

        private void CharacterNameInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CharacterNameInput.Text))
            {
                CharacterNamePlaceholder.IsVisible = true;
            }
        }
        
        // --- VAULT INTERACTIONS ---

        private void VaultItem_DoubleClick(object sender, TappedEventArgs e)
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
            if (sender is Button btn && btn.DataContext is Character character)
            {
                string folderPath = _saveService.GetCharacterFolder(character.Name);
                if (Directory.Exists(folderPath))
                {
                    OpenFolder(folderPath);
                }
            }
        }

        private static void OpenFolder(string folderPath)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = folderPath,
                        UseShellExecute = true
                    });
                    return;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", folderPath);
                    return;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", folderPath);
                }
            }
            catch
            {
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
                ShowOverlay("DATA LOAD FAILURE", $"Critical error: {ex.Message}", null);
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
            UpdatePortraitUI();

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
            OverlayBlur.IsVisible = false;

            MainModeTab.SelectedIndex = 1;
            BuilderTabControl.SelectedIndex = 0; // Start at Race Selection
        }

        private void LevelSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
             if (LevelSelector.SelectedItem is ComboBoxItem item && int.TryParse(item.Content?.ToString(), out int lvl))
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
            PointsRemainingLabel.Foreground = remaining < 0 ? Brushes.Crimson : new SolidColorBrush(Color.Parse("#007ACC"));

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

            int totalBonus = 0;

            // Race Bonuses
            if (_activeCharacter.Race != null)
            {
                if (_activeCharacter.Race.AbilityBonuses.ContainsKey(key))
                    totalBonus += _activeCharacter.Race.AbilityBonuses[key];
                
                if (_activeCharacter.Race.SelectedScores != null)
                    totalBonus += _activeCharacter.Race.SelectedScores.Count(s => s == key);
            }

            // Class Bonuses
            if (_activeCharacter.Class != null && _activeCharacter.Class.AbilityBonuses.ContainsKey(key))
            {
                totalBonus += _activeCharacter.Class.AbilityBonuses[key];
            }

            // Subclass Bonuses
            if (_activeCharacter.Subclass != null && _activeCharacter.Subclass.AbilityBonuses.ContainsKey(key))
            {
                totalBonus += _activeCharacter.Subclass.AbilityBonuses[key];
            }

            int totalScore = baseScore + totalBonus;
            
            // Large Number: Base Points (including point buy additions)
            valLbl.Text = baseScore.ToString();
            
            // Small Number (Top Right): Sum of all racial/class/subclass bonuses
            if (totalBonus > 0)
            {
                bonusLbl.Text = $"+{totalBonus}";
                bonusLbl.IsVisible = true;
                valLbl.Foreground = Brushes.LightGreen; 
            }
            else
            {
                bonusLbl.IsVisible = false;
                valLbl.Foreground = Brushes.White;
            }

            // Modifier Calculation: (Base + Bonus - 10) / 2
            int mod = (totalScore - 10) / 2;
            if (totalScore < 10 && (totalScore - 10) % 2 != 0) mod--; 

            modLbl.Text = (mod >= 0 ? "+" + mod : mod.ToString()) + $" to {key} throws";
        }

        // --- OVERLAY SYSTEM ---

        private void ShowOverlay(string title, string message, Action? onConfirm, bool showInput = false, string defaultInput = "", bool showSimple = false)
        {
            OverlayTitle.Text = title;
            OverlayMessage.Text = message;
            OverlayInput.Text = defaultInput;
            OverlayInput.IsVisible = showInput;
            OverlayAltBtn.IsVisible = showSimple;
            _pendingOverlayAction = onConfirm;
            OverlayBlur.IsVisible = true;
            if (onConfirm == null) 
            {
                OverlayConfirmBtn.Content = "OK";
                OverlayConfirmBtn.IsVisible = true; 
            }
            else 
            {
                OverlayConfirmBtn.Content = "CONFIRM";
            }
        }

        private void UpdateImage(Image imageControl, string path)
        {
            // Removed legacy class/race image update logic
        }

        private async void Portrait_Click(object sender, TappedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_activeCharacter.Name))
            {
                ShowOverlay("NAME REQUIRED", "Please name your character before choosing a portrait.", null);
                return;
            }

            if (StorageProvider == null)
            {
                ShowOverlay("ERROR", "File picker is not available on this platform.", null);
                return;
            }

            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Choose Character Portrait",
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new("Image Files")
                    {
                        Patterns = new[] { "*.jpg", "*.jpeg", "*.png" }
                    }
                }
            });

            if (files.Count > 0)
            {
                try
                {
                    var sourcePath = files[0].TryGetLocalPath();
                    if (string.IsNullOrWhiteSpace(sourcePath))
                    {
                        ShowOverlay("ERROR", "Selected file is not a local path.", null);
                        return;
                    }

                    string charFolder = _saveService.GetCharacterFolder(_activeCharacter.Name);
                    string imageFolder = Path.Combine(charFolder, "Images");
                    if (!Directory.Exists(imageFolder)) Directory.CreateDirectory(imageFolder);

                    string fileName = "portrait" + Path.GetExtension(sourcePath);
                    string destPath = Path.Combine(imageFolder, fileName);

                    File.Copy(sourcePath, destPath, true);
                    _activeCharacter.PortraitPath = destPath;

                    UpdatePortraitUI();
                }
                catch (Exception ex)
                {
                    ShowOverlay("ERROR", $"Failed to copy image: {ex.Message}", null);
                }
            }
        }

        private void UpdatePortraitUI()
        {
            if (string.IsNullOrEmpty(_activeCharacter.PortraitPath) || !File.Exists(_activeCharacter.PortraitPath))
            {
                PortraitBrushHeader.Source = null;
                return;
            }

            try
            {
                using var stream = File.OpenRead(_activeCharacter.PortraitPath);
                PortraitBrushHeader.Source = new Bitmap(stream);
            }
            catch { PortraitBrushHeader.Source = null; }
        }

        private void ConfirmOverlay_Click(object sender, RoutedEventArgs e)
        {
            _pendingOverlayAction?.Invoke();
            OverlayBlur.IsVisible = false;
        }

        private void CloseOverlay_Click(object sender, RoutedEventArgs e) => OverlayBlur.IsVisible = false;

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
                    RaceOptionsPanel.IsVisible = true;

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
                    RaceOptionsPanel.IsVisible = false;
                    ScoreSelectorsList.ItemsSource = null;
                }
            }
        }

        private void ScoreSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (RaceList.SelectedItem is not Race race) return;
            if (sender is not ComboBox changedCb) return;

            // Prevent infinite loop from re-binding
            if (ScoreSelectorsList.ItemsSource == null) return;
            
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

        public void RaceList_MouseDoubleClick(object sender, TappedEventArgs e)
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
                 RaceList.ItemsSource = null;
                 RaceList.ItemsSource = currentList.ToList();
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

        public void ClassList_MouseDoubleClick(object sender, TappedEventArgs e)
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
                 ClassList.ItemsSource = null;
                 ClassList.ItemsSource = currentList.ToList();
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
                SubclassSelectionCount.IsVisible = false;
                NoSubclassMessage.IsVisible = false;
                return;
            }

            var availableSubclasses = _allSubclasses
                .Where(s => s.ParentClass == _activeCharacter.Class.Name)
                .ToList();

            SubclassList.ItemsSource = availableSubclasses;
            SubclassSelectionCount.IsVisible = true;
            NoSubclassMessage.IsVisible = availableSubclasses.Count == 0;

            int selectedCount = availableSubclasses.Count(s => s.IsSelected);
            SubclassSelectionCount.Text = $"{selectedCount}/1 Selected";
            SubclassSelectionCount.Foreground = selectedCount > 0 ? Brushes.LightGreen : Brushes.Orange;
        }

        private void SubclassList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SubclassList.SelectedItem is Subclass sub)
            {
                SubclassDescription.Text = sub.Description;
            }
        }

        public void SubclassList_MouseDoubleClick(object sender, TappedEventArgs e)
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
                 SubclassList.ItemsSource = null;
                 SubclassList.ItemsSource = currentList.ToList();
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

        
    }
}