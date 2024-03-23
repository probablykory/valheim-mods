using Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace MoreJewelry
{
    // Custom setting drawer for Perception's Locations setting 
    public static class ConfigDrawers
    {
        public static Action<ConfigEntryBase> DrawLocationsConfigTable()
        {
            if (!CmAPI.IsLoaded()) return null;

            return cfg =>
            {
                List<string> newLocs = new List<string>();
                bool wasUpdated = false;

                int RightColumnWidth = CmAPI.RightColumnWidth;

                GUILayout.BeginVertical();

                List<string> locs = ((string)cfg.BoxedValue).Split(':').ToList();

                foreach (var loc in locs)
                {
                    GUILayout.BeginHorizontal();

                    string newLoc = GUILayout.TextField(loc, new GUIStyle(GUI.skin.textField) { fixedWidth = CmAPI.RightColumnWidth - 21 - 21 - 9 });
                    string name = string.IsNullOrEmpty(newLoc) ? loc : newLoc;
                    wasUpdated = wasUpdated || name != loc;

                    if (GUILayout.Button("x", new GUIStyle(GUI.skin.button) { fixedWidth = 21 }))
                    {
                        wasUpdated = true;
                    }
                    else
                    {
                        newLocs.Add(name);
                    }

                    if (GUILayout.Button("+", new GUIStyle(GUI.skin.button) { fixedWidth = 21 }))
                    {
                        wasUpdated = true;
                        newLocs.Add("<Location Name>");
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();

                if (wasUpdated)
                {
                    cfg.BoxedValue = string.Join(":", newLocs);
                }
            };
        }
    }
}
