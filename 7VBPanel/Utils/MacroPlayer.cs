using _7VBPanel.Instances;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;

namespace _7VBPanel.Utils
{
    public class MacroEvent
    {
        public string type { get; set; }
        public string key { get; set; }
        public bool pressed { get; set; }
        public double time { get; set; }
    }

    public static class MacroPlayer
    {
        private static readonly Dictionary<string, List<MacroEvent>> Cache = new Dictionary<string, List<MacroEvent>>(StringComparer.OrdinalIgnoreCase);
        private static readonly string[] ReleaseKeys = { "w", "a", "s", "d", "b", "e", "4", "5", "l", "o", "p" };

        public static List<MacroEvent> LoadMacro(string side)
        {
            string fileName = $"macro_{side}.json";
            if (Cache.TryGetValue(fileName, out var cached))
                return cached;

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Макрос не найден: {path}");

            string json = File.ReadAllText(path);
            var events = JsonConvert.DeserializeObject<List<MacroEvent>>(json) ?? new List<MacroEvent>();
            Cache[fileName] = events;
            return events;
        }

        public static void Execute(CS2Instance client, string side, IntPtr hwnd, Func<bool> shouldContinue)
        {
            if (client == null || string.IsNullOrEmpty(side) || hwnd == IntPtr.Zero)
                return;

            var events = LoadMacro(side);
            var sim = client.inputSimulator ?? new InputSimulator();
            var start = DateTime.UtcNow;

            foreach (var ev in events)
            {
                if (shouldContinue != null && !shouldContinue())
                    break;

                while ((DateTime.UtcNow - start).TotalSeconds < ev.time)
                {
                    if (shouldContinue != null && !shouldContinue())
                    {
                        ReleaseAllKeys(sim);
                        return;
                    }
                    Thread.Sleep(1);
                }

                // Держим фокус на одном окне — иначе клавиши уходят в другое CS2
                Win32.BringWindowToFront(hwnd);
                Thread.Sleep(15);

                if (!TryMapKey(ev.key, out VirtualKeyCode vk))
                    continue;

                if (ev.pressed)
                    sim.Keyboard.KeyDown(vk);
                else
                    sim.Keyboard.KeyUp(vk);
            }

            ReleaseAllKeys(sim);
        }

        private static void ReleaseAllKeys(InputSimulator sim)
        {
            foreach (var k in ReleaseKeys)
            {
                if (TryMapKey(k, out VirtualKeyCode vk))
                    sim.Keyboard.KeyUp(vk);
            }
        }

        private static bool TryMapKey(string key, out VirtualKeyCode vk)
        {
            vk = VirtualKeyCode.NONAME;
            if (string.IsNullOrEmpty(key))
                return false;

            switch (key.ToLowerInvariant())
            {
                case "w": vk = VirtualKeyCode.VK_W; return true;
                case "a": vk = VirtualKeyCode.VK_A; return true;
                case "s": vk = VirtualKeyCode.VK_S; return true;
                case "d": vk = VirtualKeyCode.VK_D; return true;
                case "b": vk = VirtualKeyCode.VK_B; return true;
                case "e": vk = VirtualKeyCode.VK_E; return true;
                case "l": vk = VirtualKeyCode.VK_L; return true;
                case "o": vk = VirtualKeyCode.VK_O; return true;
                case "p": vk = VirtualKeyCode.VK_P; return true;
                case "0": vk = VirtualKeyCode.VK_0; return true;
                case "1": vk = VirtualKeyCode.VK_1; return true;
                case "2": vk = VirtualKeyCode.VK_2; return true;
                case "3": vk = VirtualKeyCode.VK_3; return true;
                case "4": vk = VirtualKeyCode.VK_4; return true;
                case "5": vk = VirtualKeyCode.VK_5; return true;
                default: return false;
            }
        }
    }
}
