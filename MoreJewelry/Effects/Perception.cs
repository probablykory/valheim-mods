using HarmonyLib;
using Jewelcrafting;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoreJewelry
{
    // A configurable version of Guidance/Legacy
    public class Perception : SE_Stats
    {
        private static GameObject questObjectiveVfx = null;

        private static List<string> Locations = new List<string>() {
            "Vendor_BlackForest",
            "Hildir_camp",
        };

        static Perception()
        {
            questObjectiveVfx = AccessTools.Field(typeof(API.GemInfo).Assembly.GetType("Jewelcrafting.GemEffectSetup"), "legacyRingHildirQuest")?.GetValue(null) as GameObject;
        }

        public Perception()
        {
            m_name = "$jc_se_perception";
            m_tooltip = m_name + "_description";
        }

        public static Perception GetInstance()
        {
            var p = ScriptableObject.CreateInstance<Perception>();
            p.name = "Custom_SE_Perception_PK";
            return p;
        }

        public static void SetLocations(string locations)
        {
            if (!string.IsNullOrWhiteSpace(locations))
            {
                Locations = locations.Split(':').ToList();
            }
        }

        public override void UpdateStatusEffect(float dt)
        {
            m_tickTimer += dt;
            if (m_tickTimer >= MoreJewelry.perceptionCooldown.Value)
            {
                Transform playerPosition = Player.m_localPlayer.transform;
                if (playerPosition.position.y < 3500 && Locations.Count > 0)
                {
                    Dictionary<Vector3, string> potentialLocations = new Dictionary<Vector3, string>();
                    foreach( string name in Locations)
                    {
                        if (ZoneSystem.instance.FindClosestLocation(name, playerPosition.position, out ZoneSystem.LocationInstance loc ))
                        {
                            potentialLocations.Add(loc.m_position, name);
                        }
                    }

                    Vector3 closestLocation = new Vector3(1000000, 1000000, 1000000);
                    foreach (KeyValuePair<Vector3, string> location in potentialLocations)
                    {
                        if (Locations.Contains(location.Value))
                        {
                            if (global::Utils.DistanceXZ(closestLocation, playerPosition.position) > global::Utils.DistanceXZ(location.Key, playerPosition.position))
                            {
                                closestLocation = location.Key;
                            }
                        }
                    }

                    if (global::Utils.DistanceXZ(closestLocation, playerPosition.position) < MoreJewelry.perceptionMinDistance.Value)
                    {
                        var direction = closestLocation - playerPosition.position;
                        direction.y = 0f;
                        Instantiate(questObjectiveVfx, playerPosition.position + playerPosition.forward * 2 + playerPosition.up, Quaternion.LookRotation(direction));
                    }
                }

                m_tickTimer = 0;
            }
            base.UpdateStatusEffect(dt);
        }
    }
}
