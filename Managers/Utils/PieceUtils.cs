using ItemManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Managers
{
    public static class PieceUtils
    {
        private static readonly Dictionary<string, PieceTable> PieceTableMap = new Dictionary<string, PieceTable>();
        private static readonly Dictionary<string, string> PieceTableNameMap = new Dictionary<string, string>();

        private static Dictionary<string, Piece> PieceLookup = null;

        private static void LoadPieceTables()
        {
            foreach (var item in ObjectDB.instance.m_items)
            {
                var table = item.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces;

                if (table != null)
                {
                    PieceTableMap[table.name] = table;
                    PieceTableNameMap[item.name] = table.name;
                }
            }
        }

        private static void LoadPieceLookups()
        {
            foreach (var pair in PieceTableMap)
            {
                foreach (var obj in pair.Value.m_pieces)
                {
                    Piece piece = obj.GetComponent<Piece>();
                    if (piece == null)
                        continue;

                    PieceLookup[obj.name] = piece;
                }
            }
        }

        public static void PieceLookupRefresh()
        {
            PieceTableMap.Clear();
            PieceTableNameMap.Clear();
            PieceLookup.Clear();

            LoadPieceTables();
            LoadPieceLookups();
        }

        public static Piece GetPiece(string pieceName)
        {
            if (PieceLookup == null)
            {
                PieceLookup = new();
                PieceLookupRefresh();
            }

            if (string.IsNullOrEmpty(pieceName)) throw new ArgumentNullException(nameof(pieceName));

            if (PieceLookup.TryGetValue(pieceName, out var piece))
                return piece;

            return null;
        }
    }
}
