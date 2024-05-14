using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PieceManager;

public static partial class PiecePrefabManager
{
    public static GameObject RegisterPrefab(GameObject prefab)
    {
        piecePrefabs.Add(prefab);

        return prefab;
    }
}
