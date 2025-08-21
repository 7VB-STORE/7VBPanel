using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using _7VBPanel.Utils;
using System.Text.RegularExpressions;
using System.IO.Compression;
using _7VBPanel.Resources;

namespace _7VBPanel.Managers
{
    public static class SettingsManager
    {
        private static string settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "settings.json");

        private static readonly Dictionary<string, object> DefaultSettings = new Dictionary<string, object>
        {
            { nameof(CS2Path), "None" },
            { nameof(SteamPath), "None" },
            { nameof(CS2Arguments), "-swapcores -windowed -noborder -high -nohltv -nojoy -nosound -noaafonts -noaafonts2 -noipx -noubershader -nod3d9ex -novid -cl_forcepreload 1 +violence_hblood 0 +sethdmodels 0 +r_dynamic 0 +cl_disablehtmlmotd 1 +mat_disable_fancy_blending 1 +engine_no_focus_sleep 120" }
        };

        public static void SaveSettings()
        {
            try
            {
                if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings")))
                { 
                    Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings"));
                    UnpackCSFiles();
                }
                PropertyInfo[] properties = typeof(SettingsManager).GetProperties(BindingFlags.Public | BindingFlags.Static);
                var settings = new JObject();
                foreach (var property in properties)
                {
                    object value = property.GetValue(null);
                    var parameterlessConstructor = property.PropertyType.GetConstructor(Type.EmptyTypes);

                    settings[property.Name] = JToken.FromObject(value);
                }
                string json = settings.ToString(Formatting.Indented);
              
                File.WriteAllText(settingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении настроек: {ex.Message}");
            }
        }
        private static void UnpackCSFiles()
        {
            byte[] zipBytes = Files.CSFiles;
            using (MemoryStream zipStream = new MemoryStream(zipBytes))
            {
                using (ZipArchive archive = new ZipArchive(zipStream))
                {
                    archive.ExtractToDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings"));
                }
            }
        }
        public static void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsFilePath))
                {
                    string json = File.ReadAllText(settingsFilePath);
                    var settings = JsonConvert.DeserializeObject<JObject>(json);
                    PropertyInfo[] properties = typeof(SettingsManager).GetProperties(BindingFlags.Public | BindingFlags.Static);
                    foreach (var property in properties)
                    {
                        if (settings.TryGetValue(property.Name, out JToken value))
                        {
                            object typedValue = value.ToObject(property.PropertyType);
                            property.SetValue(null, typedValue);
                        }
                    }
                }
                ApplyDefaultSettings();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке настроек: {ex.Message}");
            }
        }

        private static void ApplyDefaultSettings()
        {
            PropertyInfo[] properties = typeof(SettingsManager).GetProperties(BindingFlags.Public | BindingFlags.Static);
            foreach (var property in properties)
            {
                object value = property.GetValue(null);

                bool isDefaultValue = value == null ||
                                      (value is string str && string.IsNullOrEmpty(str)) ||
                                      (property.PropertyType.IsValueType && Activator.CreateInstance(property.PropertyType).Equals(value));

                if (isDefaultValue && DefaultSettings.TryGetValue(property.Name, out object defaultValue))
                {
                    property.SetValue(null, defaultValue);
                }
            }
            SaveSettings();
        }



        public static string GetVideoFileSettings()
        {
            string[] lines = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "cs2_video.txt"));

           
            string vendorIDPattern = @"""VendorID""\s*""[^""]*""";
            string deviceIDPattern = @"""DeviceID""\s*""[^""]*""";
            int vendorID = 0;
            int deviceID = 0;
            HardwareUtils.GetVendorAndDeviceID(out vendorID, out deviceID);
            string updatedConfig = Regex.Replace(string.Join(Environment.NewLine, lines), vendorIDPattern, $@"""VendorID""    ""{vendorID.ToString()}""");
            updatedConfig = Regex.Replace(updatedConfig, deviceIDPattern, $@"""DeviceID""    ""{deviceID.ToString()}""");
            return updatedConfig;
        }

        public static string GetMachineConvarsFileSettings()
        {
            string[] lines = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "cs2_machine_convars.vcfg"));
            return string.Join(Environment.NewLine, lines);
        }
        public static string GetAutoExecFileSettings()
        {
            string[] lines = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "boost.cfg"));
            return string.Join(Environment.NewLine, lines);
        }

        public static string CS2Path { get; set; }
        public static string SteamPath { get; set; }
        public static string CS2Arguments { get; set; }


    }
}
