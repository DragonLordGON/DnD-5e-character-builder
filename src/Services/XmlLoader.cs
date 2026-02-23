using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DndCharacterBuilder.Models;

namespace DndCharacterBuilder.Services
{
    public class XmlLoader
    {
        private string _basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

        public List<Race> LoadRaces(bool allowHomebrew)
        {
            var races = new List<Race>();
            string path = Path.Combine(_basePath, "Races");

            if (!Directory.Exists(path)) return races;

            foreach (var file in Directory.GetFiles(path, "*.xml"))
            {
                var doc = XDocument.Load(file);
                foreach (var el in doc.Descendants("element"))
                {
                    string source = el.Attribute("source")?.Value ?? "Homebrew";
                    if (!allowHomebrew && source != "PHB") continue;

                    var race = new Race {
                        Id = el.Attribute("id")?.Value,
                        Name = el.Attribute("name")?.Value,
                        Source = source,
                        Description = el.Element("description")?.Value
                    };
                    races.Add(race);
                }
            }
            return races;
        }
    }
}