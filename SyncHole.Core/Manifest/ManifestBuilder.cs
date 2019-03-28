using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SyncHole.Core.Manifest
{
    public static class ManifestBuilder
    {
        public static async Task SaveManifestAsync(this ManifestCollection manifest, string manifestPath)
        {
            var properties = GetOrderedProperties();

            //read the file
            using (var fs = new FileStream(manifestPath, FileMode.Create))
            {
                //start writing to file
                var writer = new StreamWriter(fs);

                //write CSV header
                var header = PrintCSVHeader(properties);
                await writer.WriteLineAsync(header);

                foreach (var entry in manifest)
                {
                    //write CSV line
                    var itemLine = PrintCSVItem(properties, entry);
                    await writer.WriteLineAsync(itemLine);
                }

                //close the file
                await writer.FlushAsync();
                writer.Close();
            }
        }

        public static async Task LoadManifestAsync(this ManifestCollection manifest, string manifestPath)
        {
            var manifestInfo = new FileInfo(manifestPath);
            if (!manifestInfo.Exists)
            {
                //create an empty file
                manifestInfo.CreateText().Close();
                return;
            }

            var properties = GetOrderedProperties();

            //read the file
            using (var fs = File.OpenText(manifestPath))
            {
                //read line by line
                var line = await fs.ReadLineAsync();

                //read until EOF is not reached
                while (!fs.EndOfStream)
                {
                    //skip comment lines
                    if (line.StartsWith("%") || string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    //create manifest entry
                    var manifestItem = ParseCSVItem(properties, line);
                    manifest.Add(manifestItem);
                }

                //close the file
                fs.Close();
            }
        }

        private static ManifestItem ParseCSVItem(IReadOnlyCollection<PropertyInfo> properties, string line)
        {
            var tokens = line.Split(';');
            if (tokens.Length != properties.Count)
            {
                throw new DataMisalignedException();
            }

            var result = new ManifestItem();
            var index = 0;
            foreach (var property in properties)
            {
                property.SetValue(result, tokens[index++]);
            }

            return result;
        }

        private static string PrintCSVItem(IEnumerable<PropertyInfo> properties, ManifestItem entry)
        {
            //build the line
            var lineBuiler = new StringBuilder();
            foreach (var property in properties)
            {
                var value = property.GetValue(entry);
                lineBuiler.Append($"{value};");
            }

            //remove last comma
            lineBuiler.Length--;
            return lineBuiler.ToString();
        }

        private static string PrintCSVHeader(IEnumerable<PropertyInfo> properties)
        {
            //prepare header
            var builer = new StringBuilder();
            builer.Append('%');

            //write CSV header as a comment
            foreach (var property in properties)
            {
                builer.Append($"{property.Name};");
            }

            //remove last comma
            builer.Length--;
            return builer.ToString();
        }

        private static List<PropertyInfo> GetOrderedProperties()
        {
            return typeof(ManifestItem)
                .GetProperties()
                .Select(p => new { Property = p, Position = p.GetCustomAttributes(typeof(PositionAttribute), false).FirstOrDefault() as PositionAttribute })
                .Where(pair => pair.Position != null)
                .OrderBy(pair => pair.Position.Index)
                .Select(pair => pair.Property)
                .ToList();
        }
    }
}
