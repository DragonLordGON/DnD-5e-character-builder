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
    }

    public class CharacterClass
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string HitDie { get; set; } = "d8";
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