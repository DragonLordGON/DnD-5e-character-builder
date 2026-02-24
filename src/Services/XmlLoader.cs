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
                
                // Handle new format
                if (doc.Root?.Name.LocalName == "Races")
                {
                    foreach (var el in doc.Root.Elements("Race"))
                    {
                        var race = new Race {
                            Name = el.Element("Name")?.Value ?? "Unknown",
                            Description = el.Element("Description")?.Value ?? "",
                            Source = "FPHb",
                            CantripsCount = int.TryParse(el.Element("Cantrips")?.Value, out int c) ? c : 0,
                            ScoreSelectCount = int.TryParse(el.Element("ScoreSelect")?.Value, out int s) ? s : 0,
                            SpellList = el.Element("SpellList")?.Value ?? "",
                            AbilityBonuses = ParseStats(el.Element("Stats")?.Value)
                        };

                        var passives = el.Element("Passives")?.Value;
                        if (!string.IsNullOrEmpty(passives))
                        {
                            race.Passives = passives.Split(',').Select(p => p.Trim()).ToList();
                        }

                        var unlocks = el.Element("LevelUnlocks");
                        if (unlocks != null)
                        {
                            foreach (var u in unlocks.Elements("Unlock"))
                            {
                                if (int.TryParse(u.Attribute("level")?.Value, out int lvl))
                                {
                                    race.Unlocks.Add(new LevelUnlock { Level = lvl, Description = u.Value });
                                }
                            }
                        }

                        races.Add(race);
                    }
                }
                else
                {
                    // Legacy code path if file is old format
                    if (doc.Root != null)
                    {
                        foreach (var el in doc.Descendants("element"))
                        {
                            var race = new Race {
                                Id = el.Attribute("id")?.Value ?? "",
                                Name = el.Attribute("name")?.Value ?? "Unknown",
                                Source = el.Attribute("source")?.Value ?? "Homebrew",
                                Description = el.Element("description")?.Value ?? ""
                            };
                             races.Add(race);
                        }
                    }
                }
            }
            return races;
        }
        public List<Subclass> LoadSubclasses()
        {
            var subclasses = new List<Subclass>();
            string path = Path.Combine(_basePath, "Subclasses/Subclasses.xml");

            if (!File.Exists(path)) return subclasses;

            var doc = XDocument.Load(path);
            if (doc.Root == null) return subclasses;

            foreach (var el in doc.Root.Elements("Subclass"))
            {
                var subclass = new Subclass
                {
                    Name = el.Element("Name")?.Value ?? "Unknown",
                    ParentClass = el.Element("ParentClass")?.Value ?? "Unknown",
                    Description = el.Element("Description")?.Value ?? ""
                };

                foreach (var u in el.Elements("Unlock"))
                {
                    if (int.TryParse(u.Attribute("level")?.Value, out int lvl))
                    {
                        subclass.Unlocks.Add(new LevelUnlock { Level = lvl, Description = u.Value });
                    }
                }
                subclasses.Add(subclass);
            }
            return subclasses;
        }
        private Dictionary<string, int> ParseStats(string? stats)
        {
            var result = new Dictionary<string, int>();
            if (string.IsNullOrWhiteSpace(stats)) return result;

            foreach (var part in stats.Split(','))
            {
                var trimmed = part.Trim();
                int opIndex = trimmed.IndexOfAny(new[] { '+', '-' });
                if (opIndex > 0)
                {
                    string key = trimmed.Substring(0, opIndex).Trim();
                    // Need to handle "STR+1" vs "Any+1"
                    if (int.TryParse(trimmed.Substring(opIndex), out int val))
                    {
                        if (result.ContainsKey(key)) result[key] += val;
                        else result[key] = val;
                    }
                }
            }
            return result;
        }

        public List<CharacterClass> LoadClasses()
        {
            var classes = new List<CharacterClass>();
            string path = Path.Combine(_basePath, "Classes");

            if (!Directory.Exists(path)) return classes;

            foreach (var file in Directory.GetFiles(path, "*.xml"))
            {
                var doc = XDocument.Load(file);

                // Handle new <Classes> list format
                if (doc.Root?.Name.LocalName == "Classes")
                {
                    foreach (var el in doc.Root.Elements("Class"))
                    {
                        var charClass = new CharacterClass {
                            Name = el.Element("Name")?.Value ?? "Unknown",
                            Description = el.Element("Description")?.Value ?? "",
                            Source = "FPHb",
                            HitDie = "d" + (el.Element("HitDie")?.Value ?? "8"),
                            PrimaryAbility = el.Element("PrimaryAbility")?.Value ?? "",
                            CantripsCount = int.TryParse(el.Element("CantripsLvl1")?.Value, out int c) ? c : 0,
                            SpellList = el.Element("SpellList")?.Value ?? ""
                        };
                        
                        var saves = el.Element("SavingThrows")?.Value;
                        if (!string.IsNullOrEmpty(saves))
                        {
                            charClass.SavingThrows = saves.Split(',').Select(s => s.Trim()).ToList();
                        }
                        
                        var asis = el.Element("ASIs")?.Value;
                        if (!string.IsNullOrEmpty(asis))
                        {
                            charClass.ASIs = asis.Split(',').Select(val => int.TryParse(val.Trim(), out int lvl) ? lvl : 0).Where(l => l > 0).ToList();
                        }

                        var passives = el.Element("Passives")?.Value;
                        if (!string.IsNullOrEmpty(passives))
                        {
                            // Split comma separated list
                            charClass.Passives = passives.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).ToList();
                        }

                        var unlocks = el.Element("LevelUnlocks");
                        if (unlocks != null)
                        {
                            foreach (var u in unlocks.Elements("Unlock"))
                            {
                                if (int.TryParse(u.Attribute("level")?.Value, out int lvl))
                                {
                                    charClass.Unlocks.Add(new LevelUnlock { Level = lvl, Description = u.Value });
                                }
                            }
                        }

                        classes.Add(charClass);
                    }
                }
                else 
                {
                    // Legacy code
                    foreach (var el in doc.Descendants("element"))
                    {
                        if (el.Attribute("type")?.Value != "Class") continue;

                        var charClass = new CharacterClass {
                            Id = el.Attribute("id")?.Value ?? "",
                            Name = el.Attribute("name")?.Value ?? "Unknown",
                            Description = el.Element("description")?.Value ?? "",
                            HitDie = el.Element("hit_die")?.Value ?? "d8",
                        };
                        
                        var profs = el.Element("proficiencies");
                        if (profs != null)
                        {
                            foreach(var p in profs.Elements("proficiency"))
                            {
                                charClass.Proficiencies.Add(p.Value);
                            }
                        }

                        classes.Add(charClass);
                    }
                }
            }
            return classes;
        }
    }
}