using System;

namespace MoreJewelry
{
    public static class Effects
    {
        public static string None => string.Empty;
        public static string Aquatic => "aquatic";
        public static string Atunement => "atunement";
        public static string Awareness => "awareness";
        public static string Guidance => "guidance"; // to gacha or boss spawn location
        public static string Headhunter => "headhunter";
        public static string Legacy => "legacy"; // no map vendor or quest Locations
        public static string Lumberjack => "lumberjack";
        public static string MagicRepair => "magicRepair";
        public static string ModersBlessing => "modersBlessing";
        public static string RigidFinger => "rigidFinger";
        public static string Warmth => "warmth";
        public static string Perception => "perception";

        // maps the original JC item names to their effect names
        public static string GetEffectNameFromJcName(string jcName) => jcName switch
        {
            "JC_Necklace_Red" => Awareness,
            "JC_Necklace_Green" => MagicRepair,
            "JC_Necklace_Blue" => Aquatic, //*
            "JC_Necklace_Yellow" => Lumberjack,
            "JC_Necklace_Purple" => Guidance,
            "JC_Necklace_Orange" => Atunement,
            "JC_Ring_Purple" => RigidFinger,
            "JC_Ring_Green" => Headhunter, //*
            "JC_Ring_Red" => Warmth,
            "JC_Ring_Blue" => ModersBlessing,
            "JC_Ring_Black" => Legacy,
            _ => None
        };
    }
}
