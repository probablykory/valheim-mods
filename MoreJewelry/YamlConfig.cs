using Managers;
using LocalizationManager;
using ServerSync;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using MoreJewelry.Data;
using Logger = Managers.Logger;

namespace MoreJewelry
{
    // TODO -
    // refactor with some sort of File abstraction, allow client code to determine how swaps happen
    // pull parsing code out into a separate class, make yamlconfig 100% reusable.
    public class YamlConfig 
    {
        private static string separator = Path.DirectorySeparatorChar.ToString();
        private string filePath = "";
        private string fileName = "";

        public bool SkipSavingOfValueChange { get; set; } = false;
        public bool UseBuiltinConfig { get; set; } = false;
        public bool EnableRaisingFileEvents
        {
            get { return watcher == null ? false : watcher.EnableRaisingEvents; }
            set { if (watcher != null) { watcher.EnableRaisingEvents = value; } }
        }

        private Watcher watcher = null!;
        public CustomSyncedValue<string> SyncedValue { get; private set; } = null!;

        private object? deserializedYaml;
        //private int lastReadTextHash = 0;

        private string GetYamlFullPath() { return filePath + separator + fileName + ".yml"; }
        private bool IsSourceOfTruth() { return MoreJewelry.ConfigSync.IsSourceOfTruth; }

        public event Action? YamlConfigChanged;
        public ParsedResult Config { get; private set; }

        public YamlConfig(string path, string file)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException("path");
            if (string.IsNullOrWhiteSpace(file)) throw new ArgumentNullException("file");

            filePath = path;
            fileName = file;

            SkipSavingOfValueChange = true;

            watcher = new Watcher(filePath, fileName + ".yml");
            watcher.EnableRaisingEvents = false;
            watcher.FileChanged += WatcherOnFileChanged;

            SyncedValue = new CustomSyncedValue<string>(MoreJewelry.ConfigSync, fileName, "");
            SyncedValue.ValueChanged += OnSyncedValueChanged;
        }

        private bool isLocalChange = false;
        private void WatcherOnFileChanged(object arg1, FileSystemEventArgs arg2)
        {
            isLocalChange = true;
            OnLoadConfigFile();
            isLocalChange = false;
        }

        /*
         * Resolves the corner-case where server eats a SyncedValueChanged if it's processing a
         * remote message enabling Yaml.  Clients end up not recieving the server's configured yaml.
         */
        private bool isRebroadcast = false;
        private void RebroadcastSyncedValue()
        {
            isRebroadcast = true;
            var delayed = () =>
            {
                SkipSavingOfValueChange = true;
                SyncedValue.Value = SyncedValue.Value;
                SkipSavingOfValueChange = false;
            };
            delayed.Delay(100).Invoke();
        }

        private void OnSyncedValueChanged()
        {
            Logger.LogDebugOnly($"OnSyncedValueChanged: isLocalChange {isLocalChange}, isSourceTruth {IsSourceOfTruth()}, value hash {SyncedValue.Value.GetStableHashCode()}");
            if (IsSourceOfTruth() && isLocalChange)
                return;

            if (isRebroadcast)
            {
                isRebroadcast = false;
                return;
            }


            if (IsSourceOfTruth() && !isLocalChange)
            {
                if (Main.IsHeadless)
                {
                    Logger.LogDebugOnly($"Headless, nonlocal change, isSourceTruth, queue a rebroadcast of synced value.");
                    RebroadcastSyncedValue();
                }

                OnSaveConfigFile(SyncedValue.Value);
            }
            else
            {
                // parse network config value
                string source = "remote yaml";
                if (TryParseConfig(source, SyncedValue.Value, out ParsedResult result))
                {
                    if (Main.IsHeadless && !isLocalChange)
                    {
                        Logger.LogDebugOnly($"Headless, nonlocal change, NOT SourceTruth, queue a rebroadcast of synced value.");
                        RebroadcastSyncedValue();
                    }

                    SkipSavingOfValueChange = true;
                    SyncedValue.AssignLocalValue(SyncedValue.Value);
                    SkipSavingOfValueChange = false;

                    Config = result;
                    YamlConfigChanged?.Invoke();

                    Logger.LogInfo($"Successfully loaded {source}");
                }
                else
                {
                    Logger.LogWarning("Ignoring the changed config file, retaining the originally loaded file.");
                }
            }
        }

        public void LoadInitialConfig(bool enabled)
        {
            watcher.EnableRaisingEvents = enabled;
            UseBuiltinConfig = !enabled;

            isLocalChange = true;
            OnLoadConfigFile();
            isLocalChange = false;
        }

        public void ReloadConfig(bool isLocal)
        {
            isLocalChange = isLocal;
            if (!IsSourceOfTruth())
            {
                OnSyncedValueChanged();
                return;
            }

            if (!UseBuiltinConfig && !File.Exists(GetYamlFullPath()))
            {
                Logger.LogDebugOnly($"{GetYamlFullPath()} does not exist. Writing defaults to disk.");
                File.Create(GetYamlFullPath()).Close();
                var contents = ReadTextFromBuiltinFile();
                WriteTextToFile(contents);
            }

            //if (UseBuiltinConfig)
            //{
            //    lastReadTextHash = 0;
            //}
            OnLoadConfigFile();
        }

        private void OnLoadConfigFile()
        {
            string text = ReadTextFromFile();
            //int textHash = text.GetStableHashCode();

            //if (lastReadTextHash != 0 && lastReadTextHash == textHash)
            //{
            //    Logger.LogInfo($"Loaded text generates identical hash to current yaml.  Skipping parse.");
            //    YamlConfigChanged?.Invoke();
            //    return;
            //}

            if (TryParseConfig(fileName + ".yml", text, out ParsedResult result))
            {
                SkipSavingOfValueChange = true;
                SyncedValue.AssignLocalValue(text);
                SkipSavingOfValueChange = false;

                Config = result;
                //lastReadTextHash = UseBuiltinConfig ? 0 : textHash;

                if (IsSourceOfTruth()) // invoke only if were source of truth
                    YamlConfigChanged?.Invoke();

                string msg = "Successfully loaded ";
                if (UseBuiltinConfig)
                    msg += $"the built-in {fileName}.defaults.yml";
                else
                    msg += $"{fileName}.yml";
                if (!IsSourceOfTruth())
                    msg += ", but skipped applying as remote configuration is active";
                Logger.LogInfo(msg);
            } else
            {
                Logger.LogWarning("Ignoring the changed config file, retaining the originally loaded file.");
            }
        }

        private bool TryParseConfig(string source, string text, out ParsedResult result)
        {
            bool hasErrors = false;

            result = null!;

            try
            {
                deserializedYaml = ReadYaml<Dictionary<string, JewelryDataUnparsed>>(text);
                result = Parse(deserializedYaml, out List<string> errors);

                if (errors.Count > 0)
                {
                    Logger.LogWarning($"Found errors in {source} config. Please review the syntax of your file:\n{string.Join("\n", errors)}");
                    hasErrors = true;
                }
            }
            catch (YamlException e)
            {
                Logger.LogWarning($"Parsing {source} config failed with an error:\n{e.Message + (e.InnerException != null ? ": " + e.InnerException.Message : "")}");
                hasErrors = true;
            }

            return !hasErrors;
        }

        private void OnSaveConfigFile(object contents)
        {
            if (!SkipSavingOfValueChange && IsSourceOfTruth())
            {
                watcher.EnableRaisingEvents = false;
                WriteTextToFile($"{contents}");
                watcher.EnableRaisingEvents = true;
            }
        }


        private string ReadTextFromBuiltinFile()
        {
            return Encoding.UTF8.GetString(Localizer.ReadEmbeddedFileBytes(fileName + ".defaults.yml"));
        }

        private string ReadTextFromFile()
        {
            string contents = "";
            string file = filePath + separator + fileName + ".yml"; //GetYamlFullPath();

            if (UseBuiltinConfig)
            {
                contents = ReadTextFromBuiltinFile();
            }
            else
            {
                try
                {
                    Logger.LogDebugOnly($"attempting to read from {fileName}");
                    contents = File.ReadAllText(file);
                }
                catch (IOException e)
                {
                    Logger.LogError($"Failed reading from {fileName} config in {filePath}, message:\n {e.Message}");
                }
            }

            return contents;
        }

        private void WriteTextToFile(string contents)
        {
            string file = GetYamlFullPath();
            string tempfile = fileName + ".temp";
            try
            {
                Logger.LogDebugOnly($"attempting to write to {tempfile}");

                File.WriteAllText(tempfile, contents);
                File.Replace(tempfile, file, null);
            }
            catch (IOException e)
            {
                Logger.LogError($"Failed writing config back to {file}, message: \n{e.Message}");
            }
        }


        private static IDeserializer? deserializer = null;
        private static ISerializer? serializer = null;

        public static T RereadYaml<T>(object? obj)
        {
            string yaml = WriteYaml(obj);
            return ReadYaml<T>(yaml);
        }

        public static T ReadYaml<T>(string text)
        {
            if (string.IsNullOrEmpty(text)) return default;

            if (deserializer == null)
            {
                deserializer = new DeserializerBuilder()
                    .WithNamingConvention(LowerCaseNamingConvention.Instance)
                    //.IgnoreUnmatchedProperties()
                    .Build();
            }

            return deserializer.Deserialize<T>(text);
        }

        public static string WriteYaml(object? obj)
        {
            if (serializer == null)
            {
                serializer = new SerializerBuilder()
                    .WithNamingConvention(LowerCaseNamingConvention.Instance)
                    .Build();
            }
            return serializer.Serialize(obj);
        }

        public static List<string> ErrorCheck(string text)
        {
            var deserializedYaml = ReadYaml<Dictionary<string, JewelryDataUnparsed>>(text);
            _ = Parse(deserializedYaml, out List<string> errors);


            return errors;
        }

        public class ParsedResult
        {
            public List<JewelryData> Jewelry;
        }

        private static Dictionary<string, object?> castToStringDict(Dictionary<object, object?> dict) => new Dictionary<string, object?>(dict.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value), StringComparer.InvariantCultureIgnoreCase);

        public static ParsedResult Parse(object? root, out List<string> errors)
        {
            ParsedResult result = new ParsedResult() { Jewelry = new List<JewelryData>() };
            errors = new List<string>();

            Dictionary<string, JewelryDataUnparsed>? rootDict = root as Dictionary<string, JewelryDataUnparsed>;
            if (rootDict == null)
            {
                if (root != null)
                {
                    errors.Add($"All top-level keys must be a mapping. Got unexpected {root.GetType()}.");
                }
                return result;
            }

            string? parseEffect(object effectObj, out EffectData effect)
            {
                effect = new EffectData();

                if (effectObj is IDictionary dict)
                {
                    StatusEffectData seResult;
                    try
                    {
                        seResult = RereadYaml<StatusEffectData>(dict);
                    }
                    catch (YamlException e)
                    {
                        return $"Found an effect definition but, but parsing failed with an error:\n{e.Message + (e.InnerException != null ? ": " + e.InnerException.Message : "")}";
                    }
                    effect.SeData = seResult;
                }
                else if (effectObj is string effectStr)
                {
                    effect.NameToResolve = effectStr;
                }
                else if (effectObj is null)
                {
                    effect = null;
                }
                else
                {
                    return $"Effect value must be either a string or a mapping of property names & values, got unexpected {effectObj} of type {effectObj?.GetType().ToString() ?? "null"}";
                }

                return null;
            }

            string parseCrafting(object craftingObj, out CraftingData craftingData)
            {
                craftingData = new CraftingData();

                if (craftingObj is IDictionary && craftingObj as Dictionary<object, object> is { })
                {
                    var dict = castToStringDict(craftingObj as Dictionary<object, object>);

                    if (dict.TryGetValue("craftingstation", out object station) && station is string)
                    {
                        craftingData.CraftingStation = (string)station;
                    }
                    else
                    {
                        return $"Crafting must contain a valid craftingstation: {station}";
                    }
                    if (dict.TryGetValue("stationlevel", out object levelStr) && levelStr is string && int.TryParse((string)levelStr, out int level))
                    {
                        craftingData.StationLevel = level;
                    }
                    else
                    {
                        //Logger.LogDebugOnly("Stationlevel could not be parsed, setting to default value of 1");
                        craftingData.StationLevel = 1;
                    }

                    if (dict.TryGetValue("maxquality", out object qualityStr) && qualityStr is string && int.TryParse((string)qualityStr, out int quality))
                    {
                        craftingData.MaxQuality = quality;
                    }
                    else
                    {
                        //Logger.LogDebugOnly("Maxquality omitted or could not be parsed.");
                        craftingData.MaxQuality = -1;
                    }

                    if (dict.TryGetValue("costs", out object costsObj))
                    {
                        if (parseCosts(costsObj!, out List<CostData> costData) is { } costError)
                            return costError;
                        else
                            craftingData.Costs = costData;
                    }
                }
                else
                {
                    return $"Crafting must be a mapping of property names & values, got unexpected {craftingObj} of type {craftingObj?.GetType().ToString() ?? "null"}";
                }
                return null;
            }

            string? parseUpgrade(object upgradeOjb, out UpgradeData upgradeData)
            {
                upgradeData = new() { Costs = new ()};

                if (upgradeOjb is Dictionary<object, object> dict)
                {
                    foreach (var kvp in dict)
                    {
                        if (int.TryParse(kvp.Key.ToString(), out int level))
                        {
                            if (parseCosts(kvp.Value!, out List<CostData> costData) is { } costError)
                                return costError;
                            else
                                upgradeData.Costs[level] = costData;
                        }
                        else
                        {
                            return $"Found upgrade but failed parsing level key {kvp.Key}";
                        }
                    }
                }
                else if (upgradeOjb is null)
                {
                    //Logger.LogDebugOnly("Upgrades block was omitted.");
                    upgradeData = null;
                }
                else
                {
                    return $"Upgrade value must be a mapping of an integer level starting at 1 for the first upgrade, got unexpected {upgradeOjb} of type {upgradeOjb?.GetType().ToString() ?? "null"}";
                }

                return null;
            }

            string? parseCosts(object costsObj, out List<CostData> costData)
            {
                costData = new List<CostData>();
                CostData costs;

                if (costsObj is Dictionary<object, object> dict)
                {
                    foreach (var kvp in dict)
                    {
                        costs = new CostData();
                        costs.Name = kvp.Key.ToString();
                        string[] parts = kvp.Value?.ToString().Split(',');
                        if (parts?.Length > 0)
                        {
                            costs.Amount = parts.Length > 0 && int.TryParse(parts[0], out int amt) ? amt : 1;
                            costs.AmountPerLevel = parts.Length > 1 && int.TryParse(parts[1], out int amtpl) ? amtpl : 0;

                            costData.Add(costs);
                        }
                        else
                        {
                            return $"Found costs but failed parsing value {kvp.Value} for key {kvp.Key}";

                        }
                    }
                }
                else
                {
                    return $"Costs value must be a mapping of property names & values, got unexpected {costsObj} of type {costsObj?.GetType().ToString() ?? "null"}";
                }

                return null;
            }

            string? parseGemColor(object colorObj, out VisualData visualData)
            {
                visualData = new VisualData() { Color = null };

                if (colorObj == null)
                    visualData.Visible = true;
                else if (colorObj is string colorStr)
                {
                    if (string.Equals("hidden", colorStr, StringComparison.InvariantCultureIgnoreCase))
                        visualData.Visible = false;
                    else if (string.Equals("visible", colorStr, StringComparison.InvariantCultureIgnoreCase))
                        visualData.Visible = true;
                    else if (ColorUtility.TryParseHtmlString(colorStr, out Color color) || ColorUtility.TryParseHtmlString("#" + colorStr, out color))
                    {
                        visualData.Color = color;
                        visualData.Visible = true;
                    }
                    else
                        return $"Color value must be a string value either of 'visible', 'hidden' or an html color, got unexpected {colorObj}";
                }
                else
                {
                    return $"Color value must be a string value either of 'visible', 'hidden' or an html color, got unexpected {colorObj} of type {colorObj?.GetType().ToString() ?? "null"}";
                }

                return null;
            }

            JewelryData item;

            foreach (var obj in rootDict)
            {
                string name = obj.Key is string ? (string)obj.Key : obj.Key.ToString();

                if (obj.Value != null && obj.Value is JewelryDataUnparsed)
                {
                    item = new JewelryData();
                    item.Name = name;
                    item.Description = obj.Value.Description;

                    if (string.IsNullOrWhiteSpace(obj.Value.Prefab))
                        errors.Add($"Prefab of {name} was empty: Prefab must contain a value.");
                    else
                        item.Prefab = obj.Value.Prefab;

                    if (parseGemColor(obj.Value.Gem!, out VisualData colorData) is { } colorError)
                        errors.Add(colorError);
                    else
                        item.Gem = colorData;

                    if (parseCrafting(obj.Value.Crafting!, out CraftingData craftingData) is { } craftingError)
                        errors.Add(craftingError);
                    else
                        item.Crafting = craftingData;

                    if (parseUpgrade(obj.Value.Upgrade!, out UpgradeData upgradeData) is { } upgradeError)
                        errors.Add(upgradeError);
                    else
                        item.Upgrade = upgradeData;

                    if (parseEffect(obj.Value.effect!, out EffectData effect) is { } effectError)
                        errors.Add(effectError);
                    else
                        item.effect = effect;
                }
                else
                {
                    errors.Add($"Expected value to be of type JewelryDataUnparsed, instead got {obj.GetType()}.");
                    return result;
                }

                result.Jewelry.Add(item);
            }

            return result;
        }
    }

    public static class Extensions
    {
        public static Action Delay(this Action func, int milliseconds = 100)
        {
            CancellationTokenSource? cancelTokenSource = null;

            return () =>
            {
                cancelTokenSource?.Cancel();
                cancelTokenSource = new CancellationTokenSource();

                Task.Delay(milliseconds, cancelTokenSource.Token)
                    .ContinueWith(t =>
                    {
                        if (!t.IsCanceled)
                        {
                            func();
                        }
                    }, TaskScheduler.Default);
            };
        }

    }
}
