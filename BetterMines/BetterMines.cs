using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using SandSailorStudio.Inventory;
using SSSGame;
using System.Text;
using static SSSGame.HarvestSpawner;

namespace BetterIronMod
{
// -------------------------------- BASE PLUGIN ----------------------------------------------------------------
    [BepInPlugin("zohangirl2013.aska.BetterMines", "Better Mines", "1.0.0")]
    public class Plugin : BasePlugin
    {
        // Other classes use this logger with 'Plugin.ModLog.LogInfo("message here");'
        internal static ManualLogSource ModLog = null!;

        public override void Load()
        {
            ModLog = Log;
            Log.LogInfo("Better Mines mod loaded successfully. Version 1.0.0");

            IronOreInventoryStackPatch.InitializeConfig(Config);
            CrawlerEggPatch.InitializeConfig(Config);
            IronDepositPatch.InitializeConfig(Config);
            CaveWallPatch.InitializeConfig(Config);

            new Harmony("zohangirl2013.aska.BetterMines").PatchAll();
        }
    }

    // ---------------------------------------IRON DEPOSIT PATCH --------------------------------------------------
    [HarmonyPatch(typeof(ItemInfoDatabase), nameof(ItemInfoDatabase.Initialize))]
    internal static class IronDepositPatch
    {
        // Configurable Numbers For Iron Deposit Patch
        private static ConfigEntry<int> ironOreMinAmount = null!;
        private static ConfigEntry<int> ironOreMaxAmount = null!;
        private static ConfigEntry<int> largeStoneMinAmount = null!;
        private static ConfigEntry<int> largeStoneMaxAmount = null!;

        internal static void InitializeConfig(ConfigFile config)
        {
            ironOreMinAmount = config.Bind(
                "Iron Deposit Modifiers",
                "Iron Ore Minimum Amount",
                1,
                "Minimum amount of iron ore received whenever a deposit awards iron ore. (This is not calculated per pickaxe hit.)");

            ironOreMaxAmount = config.Bind(
                "Iron Deposit Modifiers",
                "Iron Ore Maximum Amount",
                3,
                "Maximum amount of iron ore possible whenever a deposit awards iron ore. (This is not calculated per pickaxe hit.)");

            largeStoneMinAmount = config.Bind(
                "Iron Deposit Modifiers",
                "Large Stone Minimum Amount",
                1,
                "Minimum number of large stones received when a large stone drop occurs. (Not every mining reward will contain large stones.)");

            largeStoneMaxAmount = config.Bind(
                "Iron Deposit Modifiers",
                "Large Stone Maximum Amount",
                2,
                "Maximum number of large stones possible when a large stone drop occurs. (Not every mining reward will contain large stones.)");
        }

        [HarmonyPostfix]
        private static void IronDepositPostfix(ItemInfoDatabase __instance)
        {
            var ironDepositItem = __instance.GetItemInfo("Item_Stone_IronDeposit");
            if (ironDepositItem == null)
                return;
            var largeStoneItem = ironDepositItem.components[0];
            var ironOreItem = ironDepositItem.components[1];

            // +1 needed, makes the configured maximum possible (ASKA excludes the upper limit).
            ironOreItem.quantity = ironOreMinAmount.Value;
            ironOreItem.randomQuantity = ironOreMaxAmount.Value + 1;
            largeStoneItem.quantity = largeStoneMinAmount.Value;
            largeStoneItem.randomQuantity = largeStoneMaxAmount.Value + 1;

            Plugin.ModLog.LogInfo(
      $"Iron deposit drop settings applied successfully: " +
      $"Iron min.{ironOreItem.quantity}-max.{ironOreItem.randomQuantity - 1}, " +
      $"Stone min.{largeStoneItem.quantity}-max.{largeStoneItem.randomQuantity - 1}");
        }
    }

    [HarmonyPatch(typeof(HarvestSpawner), nameof(HarvestSpawner.Awake))]
    internal static class FullLootStackPatch
    {
        [HarmonyPostfix]
        private static void ModifyLootStacks(HarvestSpawner __instance)
        {
            var parent = __instance.transform.parent;

            if (parent == null || !parent.name.Contains("IronDeposit"))
                return;

            foreach (var bitLootTable in __instance.bitLoot)
            {
                if (bitLootTable != null &&
                    bitLootTable.lootInfo != null &&
                    (bitLootTable.lootInfo.name == "Item_Iron_Ore" ||
                    bitLootTable.lootInfo.name == "Item_Stone_Raw"))
                {
                    bitLootTable.fullLootStack = true;
                }
            }

            foreach (var pieceLootTable in __instance.pieceLoot)
            {
                if (pieceLootTable != null &&
                    pieceLootTable.lootInfo != null &&
                    (pieceLootTable.lootInfo.name == "Item_Iron_Ore" ||
                    pieceLootTable.lootInfo.name == "Item_Stone_Raw"))
                {
                    // this is required to allow vanilla changes to drop amounts
                    pieceLootTable.fullLootStack = true;
                }
            }
        }
    }
// ---------------------------------------- IRON ORE INVENTORY PATCH --------------------------------------------------------
    [HarmonyPatch(typeof(ItemContainer), nameof(ItemContainer.GetStackSize), new System.Type[] { typeof(ItemInfo) })]
    internal static class IronOreInventoryStackPatch
    {
        private static ConfigEntry<bool> playerEnabled = null!;
        private static ConfigEntry<int> playerStackSize = null!;
        private static ConfigEntry<bool> villagerEnabled = null!;
        private static ConfigEntry<int> villagerStackSize = null!;
        private static bool playerLogged = false;
        private static bool villagerLogged = false;

        internal static void InitializeConfig(ConfigFile config)
        {
            playerEnabled = config.Bind(
                "Iron Ore Inventory Stacks", "Player Inventory Enabled", true,
                "Allow a custom Iron Ore stack size in the player's personal inventory.");

            playerStackSize = config.Bind(
                "Iron Ore Inventory Stacks", "Player Iron Ore Stack Size", 3,
                "Maximum Iron Ore per player inventory stack, when enabled. Must be greater than zero.(ASKA default is 1)");

            villagerEnabled = config.Bind(
                "Iron Ore Inventory Stacks", "Villager Inventory Enabled", true,
                "Allow a custom Iron Ore stack size in villager personal inventories.");

            villagerStackSize = config.Bind(
                "Iron Ore Inventory Stacks", "Villager Iron Ore Stack Size", 3,
                "Maximum Iron Ore per villager inventory stack, when enabled. Must be greater than zero. (ASKA default is 1)");
        }

        [HarmonyPostfix]
        private static void AdjustIronOreStackSize(ItemContainer __instance, ItemInfo itemInfo, ref int __result)
        {
            // Never override a 'cannot store this item' result (0), or another item type.
            if (__result <= 0 || itemInfo == null || itemInfo.name != "Item_Iron_Ore")
                return;

            var inventory = __instance.Inventory;
            if (inventory == null)
                return;

            var owner = inventory.Owner; // The ItemCollection owner is a Unity GameObject.
            if (owner == null)
                return;

            // Use the actor's actual personal inventory, not just a matching scene parent.
            var player = owner.GetComponentInParent<PlayerInteractionAgent>();
            if (player != null)
            {
                var personalInventory = player.GetInventory();
                if (personalInventory != null && personalInventory.Pointer == inventory.Pointer)
                {
                    if (playerEnabled.Value && playerStackSize.Value > 0)
                    {
                        __result = playerStackSize.Value;

                        if (!playerLogged)
                        {
                            Plugin.ModLog.LogInfo($"Player Iron Ore inventory stack size applied: {__result}");
                            playerLogged = true;
                        }
                    }
                }
            }

            var villager = owner.GetComponentInParent<Villager>();
            if (villager != null)
            {
                var personalInventory = villager.GetInventory();
                if (personalInventory != null && personalInventory.Pointer == inventory.Pointer)
                {
                    if (villagerEnabled.Value && villagerStackSize.Value > 0)
                    {
                        __result = villagerStackSize.Value;

                        if (!villagerLogged)
                        {
                            Plugin.ModLog.LogInfo($"Villager Iron Ore inventory stack size applied: {__result}");
                            villagerLogged = true;
                        }
                    }
                }
            }
        }
    }

// -------------------------------------- CRAWLER EGG CHANCE PATCH ------------------------------------------
    [HarmonyPatch(typeof(ItemInfoDatabase), nameof(ItemInfoDatabase.Initialize))]
    internal static class CrawlerEggPatch
    {
        // Configurable Chance Numbers
        private static ConfigEntry<float> crawlerSackDrop = null!; // default chance = 20%
        private static ConfigEntry<float> resinDrop = null!; // default chance = 50%
        private static ConfigEntry<float> slimeDrop = null!; // default chance = 100%
        private static ConfigEntry<float> crawlerMagDrop = null!; // default chance = 65%

        internal static void InitializeConfig(ConfigFile config)
        {
            crawlerSackDrop = config.Bind(
                "Crawler Egg Chance Modifier", "Crawler Sack Drop Chance", 0.20f, "Drop chance from 0.00(0%) to 1.00(100%)");

            resinDrop = config.Bind(
                "Crawler Egg Chance Modifier", "Resin Drop Chance", 0.50f, "Drop chance from 0.00(0%) to 1.00(100%)");

            slimeDrop = config.Bind(
                "Crawler Egg Chance Modifier", "Fertilizer Drop Chance", 1.00f, "Drop chance from 0.00(0%) to 1.00(100%)");

            crawlerMagDrop = config.Bind(
                "Crawler Egg Chance Modifier", "Crawler Maggot Drop Chance", 0.65f, "Drop chance from 0.00(0%) to 1.00(100%)");
        }


        [HarmonyPostfix]
        private static void crawlerSackPostfix(ItemInfoDatabase __instance)
        {
            ItemInfo[] crawlerEggs = new ItemInfo[4];
            for (int index = 0; index < crawlerEggs.Length; index++)
            {
                crawlerEggs[index] = __instance.GetItemInfo(
                    "Item_Misc_CrawlerEgg" + (index + 1));
            }

            int modifiedEggs = 0;

            foreach (var crawlerEgg in crawlerEggs)
            {
                try
                {
                    // [0] = sack, [1] = resin, [2] = fertilizer, [3] = maggot
                    // don't want to rely on indexing so using FirstOrDefault()
                    crawlerEgg.components.FirstOrDefault(item => item.itemInfo.name.Contains("CrawlerSack")).chance = crawlerSackDrop.Value;
                    crawlerEgg.components.FirstOrDefault(item => item.itemInfo.name.Contains("Resin")).chance = resinDrop.Value;
                    crawlerEgg.components.FirstOrDefault(item => item.itemInfo.name.Contains("Slime")).chance = slimeDrop.Value;
                    crawlerEgg.components.FirstOrDefault(item => item.itemInfo.name.Contains("CrawlerMaggot")).chance = crawlerMagDrop.Value;
                    modifiedEggs++;
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError(
                        $"[CrawlerEgg] Error modifying crawler egg loot:{crawlerEgg.name} {ex}");
                }
            }

            if (modifiedEggs == crawlerEggs.Length)
            {
                Plugin.ModLog.LogInfo(
                    $"Crawler egg drop chances applied: Sack {crawlerSackDrop.Value:P0}, Resin {resinDrop.Value:P0}");

                Plugin.ModLog.LogInfo(
                    $"Crawler egg drop chances applied: Fertilizer {slimeDrop.Value:P0}, Maggot {crawlerMagDrop.Value:P0}");
            }
        }

        [HarmonyPatch(typeof(HarvestSpawner), nameof(HarvestSpawner.Awake))]
        internal static class ChanceComponentPatch
        {
            [HarmonyPostfix]
            private static void ModifyChanceComponent(HarvestSpawner __instance)
            {
                var parent = __instance.transform.parent;

                if (parent == null || !parent.name.Contains("CrawlerEgg"))
                    return;

                foreach (var pieceLootTable in __instance.pieceLoot)
                {
                    if(pieceLootTable == null || pieceLootTable.lootInfo == null)
                        continue;

                    // chanceComponent NEEDS to equal false if drop is 100% (1.0), 
                    // otherwise, it needs to be true or it will bug the drop
                    if (pieceLootTable.lootInfo.name == "Item_Misc_CrawlerSack")
                        pieceLootTable.chanceComponent = crawlerSackDrop.Value < 1.0f;

                    else if (pieceLootTable.lootInfo.name == "Item_Wood_Resin")
                        pieceLootTable.chanceComponent = resinDrop.Value < 1.0f;

                    else if (pieceLootTable.lootInfo.name == "Item_Junk_Slime")
                        pieceLootTable.chanceComponent = slimeDrop.Value < 1.0f;

                    else if (pieceLootTable.lootInfo.name == "Item_Food_CrawlerMaggot")
                        pieceLootTable.chanceComponent = crawlerMagDrop.Value < 1.0f;

                }
            }
        }
    }

// ------------------------------------------------ CAVE WALL PATCH ------------------------------------------------------
    [HarmonyPatch(typeof(MineResourceConfig), "GetWallResourceComposition")]
    internal static class CaveWallPatch
    {
        // Configurable Multipliers for Cave Wall Patch
        private static ConfigEntry<float> ironMultiplier = null!;
        private static ConfigEntry<float> largeStoneMultiplier = null!;
        private static ConfigEntry<float> smallStoneMultiplier = null!;
        private static bool caveWallLogged = false;
        private static bool caveWallProbeLogged = false;

        // Register these settings in the existing More Iron plugin's config.
        internal static void InitializeConfig(ConfigFile config)
        {
            ironMultiplier = config.Bind(
                "Cave Wall Resource Multipliers", "IronOreMultiplier", 2.0f, "Multiplies iron ore obtained from cave walls. 1.0 = vanilla(100%), 2.0 = double(200%), etc.");

            largeStoneMultiplier = config.Bind(
                "Cave Wall Resource Multipliers", "LargeStoneMultiplier", 2.0f, "Multiplies large stones obtained from cave walls. 1.0 = vanilla(100%), 2.0 = double(200%), etc.");

            smallStoneMultiplier = config.Bind(
                "Cave Wall Resource Multipliers", "SmallStoneMultiplier", 2.0f, "Multiplies small stones obtained from cave walls. 1.0 = vanilla(100%), 2.0 = double(200%), etc.");
        }

        [HarmonyPostfix]
        private static void AdjustWallResources(MineResourceConfig __instance, ItemManifest __0)
        {
            if (!caveWallProbeLogged)
            {
                Plugin.ModLog.LogInfo("CaveWallPatch: GetWallResourceComposition executed.");
                caveWallProbeLogged = true;
            }

            if (__instance == null || __0 == null || ironMultiplier == null)
                return;

            try
            {
                var elements = __instance.elements;

                if (elements == null)
                    return;

                foreach (var element in elements)
                {
                    if (element == null || element.resource == null)
                        continue;

                    var resource = element.resource;

                    // Identify the resource and select its corresponding multiplier.
                    float multiplier = resource.name switch
                    {
                        "Item_Iron_Ore" => ironMultiplier.Value,
                        "Item_Stone_Raw" => largeStoneMultiplier.Value,
                        "Item_Stone_SmallRaw" => smallStoneMultiplier.Value,
                        _ => 1.0f
                    };

                    if (multiplier <= 1.0f || float.IsNaN(multiplier) || float.IsInfinity(multiplier))
                        continue;

                    // Read the amount ASKA originally calculated.
                    int originalQuantity = __0.GetQuantity(resource);

                    // Don't introduce resources into walls that originally contain none.
                    if (originalQuantity <= 0)
                        continue;

                    // Calculate the new total, rounding to a whole item.
                    double calculated = originalQuantity * (double)multiplier;

                    int newQuantity = calculated >= int.MaxValue
                        ? int.MaxValue
                        : (int)System.Math.Round(
                            calculated,
                            System.MidpointRounding.AwayFromZero);

                    int additionalQuantity = newQuantity - originalQuantity;

                    // Add only the difference to the original manifest.
                    if (additionalQuantity > 0)
                    {
                        __0.AddItem(resource, additionalQuantity);

                        int updatedQuantity = __0.GetQuantity(resource);

                        if (!caveWallLogged && updatedQuantity > originalQuantity)
                        {
                            Plugin.ModLog.LogInfo(
                                $"Cave wall resource successfully modified: {resource.name} {originalQuantity} -> {updatedQuantity}");

                            Plugin.ModLog.LogInfo(
                                $"Configured cave wall multipliers: Iron x{ironMultiplier.Value:0.##}, " +
                                $"Large Stone x{largeStoneMultiplier.Value:0.##}, " +
                                $"Small Stone x{smallStoneMultiplier.Value:0.##}");

                            caveWallLogged = true;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.ModLog.LogError(
                    $"Failed to modify cave-wall resources: {ex}");
            }
        }
    }
}