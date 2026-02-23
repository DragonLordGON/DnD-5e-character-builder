using System;
using System.Collections.Generic;

namespace DndCharacterBuilder.Models
{
    // The Master Data from XML Library
    public class Race
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Source { get; set; } = "PHB";
        public string Description { get; set; } = "";
        public Dictionary<string, int> AbilityBonuses { get; set; } = new Dictionary<string, int>();
        
        // New fields for Races.xml
        public List<string> Passives { get; set; } = new List<string>();
        public int CantripsCount { get; set; }
        public string SpellList { get; set; } = "";
        public List<LevelUnlock> Unlocks { get; set; } = new List<LevelUnlock>();
    }

    public class LevelUnlock
    {
        public int Level { get; set; }
        public string Description { get; set; } = "";
    }

    public class Character {
        public string Name { get; set; } = "New Character";
        public Race? Race { get; set; }
        public CharacterClass? Class { get; set; }
        public int Level { get; set; } = 1;
        public Dictionary<string, int> BaseStats { get; set; } = new Dictionary<string, int>(); 
        public List<string> Passives { get; set; } = new List<string>();
        public List<string> KnownSpells { get; set; } = new List<string>();
    }

    public class CharacterClass
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
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
    }

    // Master Item from Library
    public class Item
    {
        public string Id { get; set; } = "";
        public string NameEn { get; set; } = "";
        public string NameFr { get; set; } = "";
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