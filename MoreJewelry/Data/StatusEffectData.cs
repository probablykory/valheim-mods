using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityStandardAssets.ImageEffects;
using YamlDotNet.Serialization;
using static CharacterAnimEvent;
using Logger = Managers.Logger;

namespace MoreJewelry.Data
{
    public static class SkillUtils
    {
        public static string FromSkill(Skills.SkillType skill)
        {
            return Localization.instance.Localize("$skill_" + skill.ToString().ToLower());
        }

        public static Skills.SkillType FromName(string englishName) => (Skills.SkillType)Math.Abs(englishName.GetStableHashCode());
    }

    [Serializable, CanBeNull]
    public class DamageTypeData
    {
        public DamageTypeData() { }

        public float Blunt { get; set; }
        public float Slash { get; set; }
        public float Pierce { get; set; }
        public float Chop { get; set; }
        public float Pickaxe { get; set; }
        public float Fire { get; set; }
        public float Frost { get; set; }
        public float Poison { get; set; }
        public float Lightning { get; set; }
        public float Spirit { get; set; }
    }

    [Serializable, CanBeNull]
    public class DamageModifierData
    {
        public DamageModifierData() { }

        public DamageModifierData(string type, string mod)
        {
            Type = type;
            Modifier = mod;
        }

        public string Type { get; set; }
        public string Modifier { get; set; }
    }

    [Serializable, CanBeNull]
    public class SkillModifierData
    {
        public SkillModifierData() { }

        public SkillModifierData(string type, float mod)
        {
            Type = type;
            Modifier = mod;
        }

        public string Type { get; set; }
        public float Modifier { get; set; }
    }

    [Serializable, CanBeNull]
    public class StatusEffectData
    {
        // Leaving these commented fields on the off-chance they'll get included in adhoc SE support

        public string? Name;
        //public string SharedName;
        //public string Category;
        //public string IconName;
        //public string CustomIcon;
        //public bool? FlashIcon;
        //public bool? CooldownIcon;
        public string? Tooltip;

        [YamlMember(Alias = "StartMsgLoc")]
        public MessageHud.MessageType? StartMessageLoc;
        [YamlMember(Alias = "StartMsg")]
        public string? StartMessage;
        [YamlMember(Alias = "StopMsgLoc")]
        public MessageHud.MessageType? StopMessageLoc;
        [YamlMember(Alias = "StopMsg")]
        public string? StopMessage;


        //[YamlMember(Alias = "TickInterval")]
        //public float? m_tickInterval;
        //public float? m_healthPerTickMinHealthPercentage;
        //public float? m_healthPerTick;
        //public float? m_healthOverTime;
        //public float? m_healthOverTimeDuration;
        //public float? m_healthOverTimeInterval = 5f;
        //public float? m_staminaOverTime;
        //public float? m_staminaOverTimeDuration;
        //public float? m_staminaDrainPerSec;
        [YamlMember(Alias = "RunStaminaDrainModifier")]
        public float? m_runStaminaDrainModifier;
        [YamlMember(Alias = "JumpStaminaModifier")]
        public float? m_jumpStaminaUseModifier;
        //public float? m_eitrOverTime;
        //public float? m_eitrOverTimeDuration;
        [YamlMember(Alias = "HealthRegen")]
        public float? m_healthRegenMultiplier = 1f;
        [YamlMember(Alias = "StaminaRegen")]
        public float? m_staminaRegenMultiplier = 1f;
        [YamlMember(Alias = "EitrRegen")]
        public float? m_eitrRegenMultiplier = 1f;

        [YamlMember(Alias = "RaiseSkill")]
        public SkillModifierData RaiseSkillMod;
        //[YamlMember(Alias = "RaiseSkillName")]
        //public Skills.SkillType? m_raiseSkill;
        //[YamlMember(Alias = "RaiseSkillModifier")]
        //public float? m_raiseSkillModifier;

        [YamlMember(Alias = "SkillLevel")]
        public SkillModifierData SkillLevelMod;
        //[YamlMember(Alias = "SkillLevel")]
        //public Skills.SkillType? m_skillLevel;
        //[YamlMember(Alias = "SkillLevelModifier")]
        //public float? m_skillLevelModifier;
        [YamlMember(Alias = "SkillLevelTwo")]
        public SkillModifierData SkillLevel2Mod;
        //[YamlMember(Alias = "SkillLevelTwo")]
        //public Skills.SkillType? m_skillLevel2;
        //[YamlMember(Alias = "SkillLevelTwoModifier")]
        //public float? m_skillLevelModifier2;

        [YamlMember(Alias = "DamageModifiers")]
        public List<DamageModifierData> DamageMods = new List<DamageModifierData>();

        [YamlMember(Alias = "AttackSkill")]
        public SkillModifierData AttackSkillMod;
        //public Skills.SkillType? m_modifyAttackSkill;
        //public float? m_damageModifier = 1f;

        //public float? m_noiseModifier;
        //public float? m_stealthModifier;
        [YamlMember(Alias = "MaxCarry")]
        public float? m_addMaxCarryWeight;
        [YamlMember(Alias = "MoveSpeed")]
        public float? m_speedModifier;
        [YamlMember(Alias = "FallSpeed")]
        public float? m_maxMaxFallSpeed;
        [YamlMember(Alias = "FallDamage")]
        public float? m_fallDamageModifier;
        //public float? m_tickTimer;
        //public float? m_healthOverTimeTimer;
        //public float? m_healthOverTimeTicks;
        //public float? m_healthOverTimeTickHP;


        [YamlMember(Alias = "AttackStaminaModifier")]
        public float m_attackStaminaUseModifier;
        [YamlMember(Alias = "BlockStaminaModifier")]
        public float m_blockStaminaUseModifier;
        [YamlMember(Alias = "DodgeStaminaModifier")]
        public float m_dodgeStaminaUseModifier;
        [YamlMember(Alias = "SwimStaminaModifier")]
        public float m_swimStaminaUseModifier;
        [YamlMember(Alias = "BaseItemStaminaModifier")]
        public float m_homeItemStaminaUseModifier;
        [YamlMember(Alias = "SneakStaminaModifier")]
        public float m_sneakStaminaUseModifier;
        [YamlMember(Alias = "RunStaminaModifier")]
        public float m_runStaminaUseModifier;

        [YamlMember(Alias = "PercentDamageModifiers")]
        public DamageTypeData PercentDamageModifiers;

        public StatusEffect ToStatusEffect()
        {
            bool hasValue(string input)
            {
                return !string.IsNullOrEmpty(input);
            }
            var se = ScriptableObject.CreateInstance<SE_Stats>();

            if (hasValue(Name))
                se.m_name = Name;
            if (hasValue(Tooltip))
                se.m_tooltip = Tooltip;

            if (hasValue(StartMessage))
                se.m_startMessage = StartMessage;
            if (StartMessageLoc.HasValue)
                se.m_startMessageType = StartMessageLoc.Value;

            if (hasValue(StopMessage))
                se.m_stopMessage = StopMessage;
            if (StopMessageLoc.HasValue)
                se.m_stopMessageType = StopMessageLoc.Value;

            if (m_runStaminaDrainModifier.HasValue)
                se.m_runStaminaDrainModifier = m_runStaminaDrainModifier.Value;
            if (m_jumpStaminaUseModifier.HasValue)
                se.m_jumpStaminaUseModifier = m_jumpStaminaUseModifier.Value;
            if (m_healthRegenMultiplier.HasValue)
                se.m_healthRegenMultiplier = m_healthRegenMultiplier.Value;
            if (m_staminaRegenMultiplier.HasValue)
                se.m_staminaRegenMultiplier = m_staminaRegenMultiplier.Value;
            if (m_eitrRegenMultiplier.HasValue)
                se.m_eitrRegenMultiplier = m_eitrRegenMultiplier.Value;


            if (RaiseSkillMod != null)
            {
                se.m_raiseSkill = GetSkillType(RaiseSkillMod.Type);
                se.m_raiseSkillModifier = RaiseSkillMod.Modifier;
            }

            if (SkillLevelMod != null)
            {
                se.m_skillLevel = GetSkillType(SkillLevelMod.Type);
                se.m_skillLevelModifier = SkillLevelMod.Modifier;
            }

            if (SkillLevel2Mod != null)
            {
                se.m_skillLevel2 = GetSkillType(SkillLevel2Mod.Type);
                se.m_skillLevelModifier2 = SkillLevel2Mod.Modifier;
            }

            if (DamageMods?.Count > 0)
            {
                se.m_mods = DamageMods.Select(m => {
                    return new HitData.DamageModPair() {
                        m_type = GetDamageType(m.Type),
                        m_modifier = GetDamageModifier(m.Modifier)
                    };
                }).ToList();
            }

            if (AttackSkillMod != null)
            {
                se.m_modifyAttackSkill = GetSkillType(AttackSkillMod.Type);
                se.m_damageModifier = AttackSkillMod.Modifier;
            }

            if (PercentDamageModifiers != null)
                se.m_percentigeDamageModifiers = GetDamageTypes(PercentDamageModifiers);

            if (m_addMaxCarryWeight.HasValue)
                se.m_addMaxCarryWeight = m_addMaxCarryWeight.Value;
            if (m_speedModifier.HasValue)
                se.m_speedModifier = m_speedModifier.Value;
            if (m_maxMaxFallSpeed.HasValue)
                se.m_maxMaxFallSpeed = m_maxMaxFallSpeed.Value;
            if (m_fallDamageModifier.HasValue)
                se.m_fallDamageModifier = m_fallDamageModifier.Value;

            return se;
        }

        /*  ItemDrop.ItemData.ItemType
            Ammo
            AmmoNonEquipable
            Bow
            Chest
            Consumable
            Customization
            Fish
            Helmet
            Legs
            Material
            Misc
            OneHandedWeapon
            Shield
            Back -> Shoulder
            Tool
            Torch
            Trophy
            TwoHandedWeapon
            Utility */
        public static string GetItemTypeString(string type)
        {
            if (string.Equals(type, "back", StringComparison.InvariantCultureIgnoreCase))
                return "shoulder";
            return type.ToLower();
        }

        /*  HitData.DamageModifier
            Normal,
		    Resistant,
		    Weak,
		    Immune,
		    Ignore,
		    Very Resistant,
		    Very Weak */
        public static string GetDamageModString(string type)
        {
            if (string.Equals(type, "very resistant", StringComparison.InvariantCultureIgnoreCase))
                return "veryresistant";
            else if (string.Equals(type, "very weak", StringComparison.InvariantCultureIgnoreCase))
                return "veryweak";
            return type.ToLower();
        }

        /*  Skills.SkillType
            None = 0,
            Swords = 1,
            Knives = 2,
            Clubs = 3,
            Polearms = 4,
            Spears = 5,
            Blocking = 6,
            Axes = 7,
            Bows = 8,
            ElementalMagic = 9,
            BloodMagic = 10,
            Unarmed = 11,
            Pickaxes = 12,
            WoodCutting = 13,
            Crossbows = 14,
            Jump = 100,
            Sneak = 101,
            Run = 102,
            Swim = 103,
            Fishing = 104,
            Ride = 110,
            All = 999 */
        public static string GetSkillTypeString(string type)
        {
            if (string.Equals(type, "fists", StringComparison.InvariantCultureIgnoreCase))
                return "Unarmed";
            if (string.Equals(type, "wood-cutting", StringComparison.InvariantCultureIgnoreCase))
                return "WoodCutting";
            if (string.Equals(type, "wood cutting", StringComparison.InvariantCultureIgnoreCase))
                return "WoodCutting";
            if (string.Equals(type, "elemental magic", StringComparison.InvariantCultureIgnoreCase))
                return "ElementalMagic";
            if (string.Equals(type, "blood magic", StringComparison.InvariantCultureIgnoreCase))
                return "BloodMagic";
            return type;
        }

        public static HitData.DamageType GetDamageType(string type)
        {
            if (Enum.TryParse(type, true, out HitData.DamageType dmgType))
                return dmgType;
            return 0;
        }

        public static HitData.DamageTypes GetDamageTypes(DamageTypeData typeData)
        {
                HitData.DamageTypes types = new HitData.DamageTypes();
                types.m_blunt = typeData.Blunt;
                types.m_slash = typeData.Slash;
                types.m_pierce = typeData.Pierce;
                types.m_chop = typeData.Chop;
                types.m_pickaxe = typeData.Pickaxe;

                types.m_fire = typeData.Fire;
                types.m_frost = typeData.Frost;
                types.m_poison = typeData.Poison;
                types.m_lightning = typeData.Lightning;
                types.m_spirit = typeData.Spirit;
                return types;
        }

        public static HitData.DamageModifier GetDamageModifier(string type)
        {
            if (Enum.TryParse(GetDamageModString(type), true, out HitData.DamageModifier dmgType))
                return dmgType;
            return 0;
        }

        public static Skills.SkillType GetSkillType(string type)
        {
            type = GetSkillTypeString(type);
            if (Enum.TryParse(type, true, out Skills.SkillType dmgType))
                return dmgType;

            dmgType = SkillUtils.FromName(type);

            return dmgType;
        }
    }
}
