using System.Collections.Generic;

namespace DndCharacterBuilder.Models
{
    public class ScoreSelector
    {
        public string Label { get; set; } = "Bonus +1";
        public List<string> Options { get; set; } = new List<string> { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
        public string SelectedOption { get; set; } = "";
    }
}