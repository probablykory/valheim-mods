using ItemManager;
using Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PieceManager;

public partial class BuildPiece
{
    public BuildPiece(string prefabToClone, string prefabName)
    {
        if (string.IsNullOrEmpty(prefabToClone)) throw new ArgumentException("param cannot be null or empty", nameof(prefabToClone));
        if (string.IsNullOrEmpty(prefabName)) throw new ArgumentException("param cannot be null or empty", nameof(prefabName));

        var prefab = PrefabManager.GetPrefab(prefabToClone);
        if (prefab == null) throw new ArgumentException($"Unable to find prefab {prefabToClone}");

        var clone = Utilities.CreateClonePrefab(prefab, prefabName);
        Prefab = PiecePrefabManager.RegisterPrefab(clone);



        registeredPieces.Add(this);
    }

    public static void EnsureRegisteredPieces()
    {
        BuildPiece.Patch_FejdStartup(null);
    }
}
