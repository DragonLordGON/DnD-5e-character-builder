using System;
using System.Collections.Generic;

namespace DndCharacterBuilder.Models
{
    // The Master Data from XML Library
    public class Race
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Source { get; set; } = "FPHb";
        public string Description { get; set; } = "";
        public Dictionary<string, int> AbilityBonuses { get; set; } = new Dictionary<string, int>();
        
        // New fields for Races.xml
        public List<string> Passives { get; set; } = new List<string>();
        public int CantripsCount { get; set; }
        public string SpellList { get; set; } = "";
        public List<LevelUnlock> Unlocks { get; set; } = new List<LevelUnlock>();
        
        // Dynamic Choice
        public int ScoreSelectCount { get; set; } // Example: 2 for Variant Human
        public List<string> SelectedScores { get; set; } = new List<string>();

        // UI Helpers
        public bool IsSelected { get; set; }
        public bool IsDisabled { get; set; }
        public System.Windows.Visibility SelectedVisibility => IsSelected ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        public System.Windows.Media.Brush BackgroundColor => IsSelected ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(9, 71, 113)) : System.Windows.Media.Brushes.Transparent;
        public System.Windows.Media.Brush BorderColor => IsSelected ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 122, 204)) : System.Windows.Media.Brushes.Transparent;
    }

    public class LevelUnlock
    {
        public int Level { get; set; }
        public string Description { get; set; } = "";
    }

    public class Character {
        public string Name { get; set; } = "";
        public Race? Race { get; set; }
        public CharacterClass? Class { get; set; }
        public Subclass? Subclass { get; set; }
        public int Level { get; set; } = 1;
        public Dictionary<string, int> BaseStats { get; set; } = new Dictionary<string, int>(); 
        public List<string> Passives { get; set; } = new List<string>();
        public List<string> KnownSpells { get; set; } = new List<string>();

        // Safe property for display binding
        public bool IsWIP { get; set; } = false;
    }

    public class CharacterClass
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Source { get; set; } = "FPHb";
        public string Description { get; set; } = "";
        public string HitDie { get; set; } = "d8";
        public string PrimaryAbility { get; set; } = "";
        public List<string> SavingThrows { get; set; } = new List<string>();
        public List<int> ASIs { get; set; } = new List<int>();
        public List<string> Passives { get; set; } = new List<string>();
        public int CantripsCount { get; set; }
        public string SpellList { get; set; } = "";
        public List<LevelUnlock> Unlocks { get; set; } = new List<LevelUnlock>();
        public List<string> Proficiencies { get; set; } = new List<string>();
        
        // UI Helpers
        public bool IsSelected { get; set; }
        public bool IsDisabled { get; set; }
        public System.Windows.Visibility SelectedVisibility => IsSelected ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        public System.Windows.Media.Brush BackgroundColor => IsSelected ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(9, 71, 113)) : System.Windows.Media.Brushes.Transparent;
        public System.Windows.Media.Brush BorderColor => IsSelected ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 122, 204)) : System.Windows.Media.Brushes.Transparent;
    }

    public class Subclass
    {
        public string Name { get; set; } = "";
        public string ParentClass { get; set; } = "";
        public string Description { get; set; } = "";
        public List<LevelUnlock> Unlocks { get; set; } = new List<LevelUnlock>();
        
        // UI Helpers
        public bool IsSelected { get; set; }
        public bool IsDisabled { get; set; }
        public System.Windows.Visibility SelectedVisibility => IsSelected ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        public System.Windows.Media.Brush BackgroundColor => IsSelected ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(9, 71, 113)) : System.Windows.Media.Brushes.Transparent;
        public System.Windows.Media.Brush BorderColor => IsSelected ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 122, 204)) : System.Windows.Media.Brushes.Transparent;
    }

    // Master Item from Library
    public class Item
    {
        public string Id { get; set; } = "";
        public string NameEn { get; set; } = "";
        public string NameFr { get; set; } = "";
        public string Source { get; set; } = "FPHb";
        public double DefaultCostGp { get; set; }
        public double Weight { get; set; } // Added Weight

        public string GetDisplayName(string lang)
        {
            if (lang == "fr")
                return !string.IsNullOrEmpty(NameFr) ? NameFr : NameEn;
            return !string.IsNullOrEmpty(NameEn) ? NameEn : NameFr;
        }
    }

    // Specific item in Player's Backpack
    public class InventoryItem
    {
        public string Name { get; set; } = "";
        public double PricePaid { get; set; }
        public double Weight { get; set; }
        public int Quantity { get; set; } = 1;
    }
}