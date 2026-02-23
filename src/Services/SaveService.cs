using System;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using DndCharacterBuilder.Models;

namespace DndCharacterBuilder.Services
{
    public class SaveService
    {
        private string _savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Characters");

        public void SaveCharacter(string filename, string name, string raceId, List<string> itemIds)
        {
            if (!Directory.Exists(_savePath)) Directory.CreateDirectory(_savePath);

            XElement root = new XElement("CharacterSheet",
                new XAttribute("generatedAt", DateTime.Now.ToString()),
                new XElement("Info",
                    new XElement("Name", name),
                    new XElement("RaceId", raceId)
                ),
                new XElement("Inventory",
                    from id in itemIds select new XElement("ItemId", id)
                )
            );

            root.Save(Path.Combine(_savePath, filename + ".xml"));
        }
    }
}