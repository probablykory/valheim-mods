using HarmonyLib;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    // Much of this code was borrowed and modified from ServerSync: https://github.com/blaxxun-boop/ServerSync
    // Blaxxun et al deserves all the credit for this.
    public class CustomSyncedValueBase
    {
        public readonly string Name;
        public readonly Type Type;
        public event Action<object, object>? ValueChanged;
        public bool IsSourceOfTruth { get; private set; } = true;

        private object? boxedValue;
        public object? BoxedValue
        {
            get => boxedValue;
            set
            {
                boxedValue = value;
                ValueChanged?.Invoke(this, value!);
            }
        }
        public object? LocalBaseValue;

        private CustomRPC customRPC = null!;

        private static readonly HashSet<CustomSyncedValueBase> SyncedValues = new HashSet<CustomSyncedValueBase>();
        private static Harmony harmony = null!;

        protected CustomSyncedValueBase(string name, Type type, object? initialValue)
        {
            Name = name;
            Type = type;
            SyncedValues.Add(this);
            if (harmony == null)
            {
                harmony = (Harmony)AccessTools.Field(typeof(Main), "Harmony").GetValue(null);
                harmony.PatchAll(typeof(Patches));
                Get.Plugin.LogDebugOnly($"Jotunn's harmony instance obtained, CustomSyncedValueBase+Patches applied.");
            }

            boxedValue = initialValue;
            customRPC = NetworkManager.Instance.AddRPC($"{Name}_CustomSyncedValue_RPC", OnServerReceive, OnClientReceive);
            SynchronizationManager.Instance.AddInitialSynchronization(customRPC, GetPackage);
        }

        public void SendPackage()
        {
            if (ZNet.instance == null)
            {
                Get.Plugin.LogDebugOnly($"SendPackage called but am not connected.");
            }

            if (!SynchronizationManager.Instance.PlayerIsAdmin)
            {
                Get.Plugin.LogDebugOnly($"SendPackage called but Player is not admin.");
            }

            if (ZNet.instance != null && SynchronizationManager.Instance.PlayerIsAdmin)
            {
                ZPackage package = GetPackage();

                if (ZNet.instance.IsClientInstance())
                {
                    customRPC.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
                }
                else
                {
                    customRPC.SendPackage(ZNet.instance.m_peers, package);
                }
            }
        }

        private ZPackage GetPackage()
        {
            return (new PackageEntry()
            {
                key = Name,
                type = Type,
                value = boxedValue,
            }).ToPackage();
        }

        private IEnumerator OnServerReceive(long sender, ZPackage package)
        {
            Get.Plugin.LogDebugOnly($"Server received RPC: {sender} {package}");
            yield return null;

            var entries = package.ReadEntries();
            if (entries.Count > 0 && entries.TryGetValue(Name, out PackageEntry entry))
            {
                BoxedValue = LocalBaseValue = entry.value;
                Get.Plugin.LogDebugOnly($"Set local and boxed: {entry.value}");

                // send to all other clients
                customRPC.SendPackage(ZNet.instance.m_peers.Where(x => x.m_uid != sender).ToList(), package);
            }
            else
            {
                OnReceiveError(entries);
            }

            yield break;
        }

        private IEnumerator OnClientReceive(long sender, ZPackage package)
        {
            Get.Plugin.LogDebugOnly($"Client received RPC: {sender} {package}");
            yield return null;

            var entries = package.ReadEntries();
            if (entries.Count > 0 && entries.TryGetValue(Name, out PackageEntry entry))
            {
                IsSourceOfTruth = false;
                LocalBaseValue ??= BoxedValue;
                BoxedValue = entry.value;

                Get.Plugin.LogDebugOnly($"Set source of truth: {IsSourceOfTruth}, {Environment.NewLine}local: {LocalBaseValue} {Environment.NewLine}boxed: {BoxedValue}");
            }
            else
            {
                OnReceiveError(entries);
            }

            yield break;
        }

        private void OnReceiveError(ParsedEntries entries)
        {
            Get.Plugin.LogWarning($"{Name}_CustomSyncedValue_RPC recieved package without expected key: {Name}");
            string result = "";
            foreach (var kvp in entries)
            {
                result += $"{kvp.Key} - {kvp.Value.type} - {kvp.Value.value?.ToString()} {Environment.NewLine}";
            }
            Get.Plugin.LogWarning($"Result: {Environment.NewLine}{result}");

        }

        private void OnResetFromServer()
        {
            // Reset value before flag to prevent unnecessary save
            BoxedValue = LocalBaseValue;
            IsSourceOfTruth = true;
            LocalBaseValue = null;
        }

        private void OnServerShutdown()
        {
            // todo anything?
        }


        private class Patches
        {
            [HarmonyPatch(typeof(ZNet), nameof(ZNet.Shutdown))]
            [HarmonyPostfix]
            private static void ZNet_Shutdown()
            {
                if (ZNet.instance.IsClientInstance())
                    foreach (CustomSyncedValueBase csv in SyncedValues)
                        csv.OnResetFromServer();
                else
                    foreach (CustomSyncedValueBase csv in SyncedValues)
                        csv.OnServerShutdown();
            }
        }
    }

    public sealed class CustomSyncedValue<T> : CustomSyncedValueBase
    {
        public T Value
        {
            get => (T)BoxedValue!;
            set
            {
                BoxedValue = value;
                if (BoxedValue != null)
                {
                    SendPackage();
                }
            }
        }

        public CustomSyncedValue(string name, T value = default!) : base(name, typeof(T), value) { }

        public void AssignLocalValue(T value)
        {
            if (IsSourceOfTruth && SynchronizationManager.Instance.PlayerIsAdmin)
            {
                Value = value;
            }
            else
            {
                LocalBaseValue = value;
            }
        }
    }

}
