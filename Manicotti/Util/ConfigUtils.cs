using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Autodesk.Revit.UI;

namespace Manicotti.Util
{
    public class LayerMap
    {
        public List<string> WALL { get; set; } = new List<string>();
        public List<string> COLUMN { get; set; } = new List<string>();
        public List<string> DOOR { get; set; } = new List<string>();
        public List<string> WINDOW { get; set; } = new List<string>();
        public List<string> OPENING { get; set; } = new List<string>();
        public List<string> SPACE { get; set; } = new List<string>();
        public List<string> FRAME { get; set; } = new List<string>();
    }

    public static class ConfigUtils
    {
        private static string GetInstallPath()
        {
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            // The post-build event copies the addin to a folder like:
            // C:\Users\<user>\AppData\Roaming\Autodesk\Revit\Addins\2022\Manicotti\
            // The JSON will be in a sub-folder of that, so we need the parent directory.
            return Path.GetDirectoryName(assemblyLocation);
        }

        public static LayerMap LoadLayerMap()
        {
            // The user's snippet used UtilGetInstallPath.Execute(). I've created a local version.
            // The user's snippet also used System.Text.Json, which is fine for modern .NET but
            // for Revit addins, Newtonsoft.Json is often more reliable if bundled.
            // I will stick to System.Text.Json as requested.
            var configPath = Path.Combine(GetInstallPath(), "Resources", "config", "layer-map.json");

            if (!File.Exists(configPath))
            {
                TaskDialog.Show("Error", "Layer map file not found at: " + configPath);
                return new LayerMap(); // Return an empty map to prevent crashing
            }

            var json = File.ReadAllText(configPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var map = JsonSerializer.Deserialize<LayerMap>(json, options);

            // Normalize all layer names to uppercase for case-insensitive comparison later
            foreach (var prop in typeof(LayerMap).GetProperties())
            {
                var list = (List<string>)prop.GetValue(map);
                if (list != null)
                {
                    prop.SetValue(map, list.Select(s => s.ToUpperInvariant()).Distinct().ToList());
                }
            }
            return map;
        }
    }
}
