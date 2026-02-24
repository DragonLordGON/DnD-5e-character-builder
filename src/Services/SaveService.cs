using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using DndCharacterBuilder.Models;

namespace DndCharacterBuilder.Services
{
    public class SaveService
    {
        // Use GetCurrentDirectory to save in the workspace root during dev, or app root when running exe
        private string _basePath = Path.Combine(Directory.GetCurrentDirectory(), "Characters");

        public SaveService()
        {
            if (!Directory.Exists(_basePath)) Directory.CreateDirectory(_basePath);
        }

        public void SaveCharacter(Character character)
        {
            if (string.IsNullOrWhiteSpace(character.Name)) return;

            string safeName = string.Join("_", character.Name.Split(Path.GetInvalidFileNameChars()));
            string charFolder = Path.Combine(_basePath, safeName);
            
            if (!Directory.Exists(charFolder)) Directory.CreateDirectory(charFolder);

            XElement root = new XElement("Character",
                new XAttribute("Version", "1.0"),
                new XElement("Name", character.Name),
                new XElement("Level", character.Level),
                new XElement("Race", character.Race?.Name ?? ""),
                new XElement("Class", character.Class?.Name ?? ""),
                new XElement("PortraitPath", character.PortraitPath),
                new XElement("IsWIP", character.IsWIP.ToString()),
                new XElement("BaseStats",
                    from kvp in character.BaseStats
                    select new XElement("Stat", new XAttribute("Name", kvp.Key), kvp.Value)
                )
            );

            root.Save(Path.Combine(charFolder, "Character.xml"));
        }

        public string GetCharacterFolder(string charName)
        {
             string safeName = string.Join("_", charName.Split(Path.GetInvalidFileNameChars()));
             return Path.Combine(_basePath, safeName);
        }

        public List<Character> LoadAllCharacters()
        {
            var characters = new List<Character>();
            if (!Directory.Exists(_basePath)) return characters;

            foreach (var dir in Directory.GetDirectories(_basePath))
            {
                string xmlPath = Path.Combine(dir, "Character.xml");
                if (File.Exists(xmlPath))
                {
                    try 
                    {
                        var doc = XDocument.Load(xmlPath);
                        var root = doc.Root;
                        if (root == null) continue;

                        var isWipElement = root.Element("IsWIP");
                        bool isWip = true; 
                        if (isWipElement != null) bool.TryParse(isWipElement.Value, out isWip);

                        var charObj = new Character
                        {
                            Name = root.Element("Name")?.Value ?? "Unknown",
                            Level = int.Parse(root.Element("Level")?.Value ?? "1"),
                            PortraitPath = root.Element("PortraitPath")?.Value ?? "",
                            IsWIP = isWip
                        };
                        
                        string raceName = root.Element("Race")?.Value ?? "Unknown";
                        if (!string.IsNullOrEmpty(raceName)) charObj.Race = new Race { Name = raceName };

                        string className = root.Element("Class")?.Value ?? "Unknown";
                        if (!string.IsNullOrEmpty(className)) charObj.Class = new CharacterClass { Name = className };
                        
                        // Load Base Stats to ensure we can recalc WIP state correctly if needed later
                        // But for now just relying on saved IsWIP flag for list display is faster
                        
                        characters.Add(charObj);
                    }
                    catch { /* Skip corrupted files */ }
                }
            }
            return characters;
        }
    }
}