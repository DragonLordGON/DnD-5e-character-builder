using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using DndCharacterBuilder.Models;
using System;

namespace DndCharacterBuilder.Services
{
    public class ItemLoader
    {
        public List<Item> LoadItems()
        {
            var items = new List<Item>();
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Items");

            if (!Directory.Exists(path)) return items;

            foreach (var file in Directory.GetFiles(path, "*.xml"))
            {
                var doc = XDocument.Load(file);
                foreach (var el in doc.Descendants("item"))
                {
                    items.Add(new Item
                    {
                        Id = el.Attribute("id")?.Value ?? "unknown",
                        NameEn = el.Element("name_en")?.Value ?? "",
                        NameFr = el.Element("name_fr")?.Value ?? "",
                        Source = "FPHb",
                        DefaultCostGp = double.Parse(el.Element("cost_gp")?.Value ?? "0"),
                        Weight = double.Parse(el.Element("weight")?.Value ?? "0")
                    });
                }
            }
            return items;
        }
    }
}