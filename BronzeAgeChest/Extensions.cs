using Common;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BronzeAgeChest
{
    public static class Extensions
    {
        public static Piece GetPiece(Piece piece)
        {
            Piece result = null;

            var pieceTables = ReflectionHelper.GetPrivateField<Dictionary<string, PieceTable>>(PieceManager.Instance, "PieceTableMap");
            foreach (var pair in pieceTables)
            {
                foreach (GameObject obj in pair.Value.m_pieces)
                {
                    var p = obj.GetComponent<Piece>();

                    if ((!string.IsNullOrEmpty(p.name)) && p.name.Equals(piece.name))
                    {
                        result = p;
                        break;
                    }
                }

                if (result != null) break;
            }

            return result;
        }

        private static Dictionary<string, CustomPiece> dictPieces;
        public static bool Update(this CustomPiece piece, PieceConfig newPiece)
        {
            if (ZNetScene.instance != null)
            {
                global::Piece p = GetPiece(piece.Piece);
                if (p == null)
                {
                    Get.Plugin.Logger.LogError($"Error updating piece {piece?.Piece?.name}, did not find existing piece in available piece tables.");
                    return false;
                }

                // Update existing piece in place.
                p.m_craftingStation = ZNetScene.instance.GetPrefab(newPiece.CraftingStation).GetComponent<CraftingStation>();
                p.m_resources = newPiece.GetRequirements();

                foreach (var res in p.m_resources)
                {
                    var prefab = ObjectDB.instance.GetItemPrefab(res.m_resItem.name.Replace("JVLmock_", ""));
                    if (prefab != null)
                    {
                        res.m_resItem = prefab.GetComponent<ItemDrop>();
                    }
                }

                // cache refernce to ItemManager.Instance.Recipes
                if (dictPieces == null)
                {
                    var pieces = ReflectionHelper.GetPrivateField<Dictionary<string, CustomPiece>>(PieceManager.Instance, "Pieces");
                    if (pieces != null)
                    {
                        dictPieces = pieces;
                    }
                }

                if (dictPieces != null)
                {
                    dictPieces.Remove(p.gameObject.name);
                    dictPieces.Add(p.gameObject.name, new CustomPiece(p.gameObject, piece.PieceTable, false));
                }

                return true;
            }
            else
            {
                PieceManager.Instance.RemovePiece(piece.PiecePrefab.name);
                PieceManager.Instance.AddPiece(new CustomPiece(piece.PiecePrefab, false, newPiece));
            }

            return false;
        }
    }
}
