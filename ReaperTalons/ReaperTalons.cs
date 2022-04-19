using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace ReaperTalons {

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI), nameof(RecalculateStatsAPI))]
    public class ReaperTalons : BaseUnityPlugin {
        // Mod metadata
        // mod won't get loaded if i change these (author or name) for some reason?
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "AuthorName";
        public const string PluginName = "ExamplePlugin";
        public const string PluginVersion = "0.0.1";

        // Item parameters
        private static ItemDef itemDefinition;
        private const int BUFF_COEFFICIENT = 2;
        private const int DAMAGE_COEFFICIENT = 3;

        public void Awake() {
            Log.Init(Logger);
            DefineItem();
            SetHooks();
            Log.LogInfo(nameof(Awake) + " done.");
        } // Awake

        /// <summary>
        /// Defines the item and adds it to the game.
        /// </summary>
        private void DefineItem() {
            itemDefinition = ScriptableObject.CreateInstance<ItemDef>();

            // in-game text data
            itemDefinition.name = "ITEM_BUFFWHENLOW_NAME";
            itemDefinition.nameToken = "ITEM_BUFFWHENLOW_NAME";
            itemDefinition.pickupToken = "ITEM_BUFFWHENLOW_PICKUP";
            itemDefinition.descriptionToken = "ITEM_BUFFWHENLOW_DESC";
            itemDefinition.loreToken = "ITEM_BUFFWHENLOW_LORE";
            AddTokens();

            itemDefinition.tier = ItemTier.Lunar;
            itemDefinition.canRemove = true;
            itemDefinition.hidden = false;

            // TODO: create custom assets
            itemDefinition.pickupIconSprite = Resources.Load<Sprite>("Textures/MiscIcons/texMysteryIcon");
            itemDefinition.pickupModelPrefab = Resources.Load<GameObject>("Prefabs/PickupModels/PickupMystery");

            var displayRules = new ItemDisplayRuleDict(null);
            R2API.ItemAPI.Add(new CustomItem(itemDefinition, displayRules)); // add to game
        } // DefineItem

        /// <summary>
        /// Sets the neccessary hooks needed for this item to function.
        /// </summary>
        private void SetHooks() {
            // On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
            // On.RoR2.HealthComponent.Heal += HealthComponent_Heal;
            R2API.RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        } // SetHooks

        private float HealthComponent_Heal(On.RoR2.HealthComponent.orig_Heal orig, HealthComponent self, float amount, ProcChainMask procChainMask, bool nonRegen) {
            ManageBuff(self);
            return orig(self, amount, procChainMask, nonRegen);
        } // HealthComponent_Heal

        private void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo) {
            ManageBuff(self);
            orig(self, damageInfo);
        } // HealthComponent_TakeDamage

        /// <summary>
        /// Apply the buff to the user.
        /// </summary>
        /// <param name="healthComponent">The entity to apply the buff to.</param>
        private void ManageBuff(HealthComponent healthComponent) {
            Log.LogDebug(nameof(ManageBuff) + " called.");
            RecalculateStatsAPI_GetStatCoefficients(healthComponent.body, new RecalculateStatsAPI.StatHookEventArgs()); // won't actually update stats :(
        } // ManageBuff

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args) {
            int itemCount = sender?.inventory.GetItemCount(itemDefinition.itemIndex) ?? 0;
            if (itemCount <= 0) {
                return;
            } // if

            float hpMissingPercent = (1 - sender.healthComponent.health / sender.healthComponent.body.maxHealth) * 100;
            float buffMultiplier = BUFF_COEFFICIENT * itemCount * hpMissingPercent / 100;

            args.damageMultAdd += buffMultiplier;
            args.attackSpeedMultAdd += buffMultiplier;
            args.moveSpeedMultAdd += buffMultiplier;
            Log.LogDebug("RECALC CALLED for " + sender.name + ". Missing " + hpMissingPercent + "% of HP resulting in a " + buffMultiplier + " buff. Current damage is: " + sender.damage);
        } // RecalculateStatsAPI_GetStatCoefficients

        private void AddTokens() {
            LanguageAPI.Add("ITEM_BUFFWHENLOW_NAME", "Reaper Talons");
            LanguageAPI.Add("ITEM_BUFFWHENLOW_PICKUP", "Grow stronger as death approaches... <color=#ff7f7f>BUT take continuous damage.</color>");
            LanguageAPI.Add("ITEM_BUFFWHENLOW_DESC", "Convert each percent of missing HP to a <style=cIsDamage>" + BUFF_COEFFICIENT + "%</style> <style=cStack>(+" + BUFF_COEFFICIENT + "% per stack)</style> increase to <style=cIsDamage>base damage</style>, <style=cIsDamage>attack speed</style>, and <style=cIsUtility>movement speed</style>, but <style=cIsHealing>lose " + DAMAGE_COEFFICIENT + "% <style=cStack>(+" + DAMAGE_COEFFICIENT + "% per stack)</style> of current HP every second</style>.");
            LanguageAPI.Add("ITEM_BUFFWHENLOW_LORE", "UNDER CONSTRUCTION!!!");
        } // AddTokens

        private void Update() {
            if (Input.GetKeyDown(KeyCode.P)) {
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(itemDefinition.itemIndex), transform.position, transform.forward * 20f);
            } // if

            if (Input.GetKeyDown(KeyCode.O)) {
                var transform = PlayerCharacterMasterController.instances[0].master;
                DamageInfo di = new() {
                    damage = transform.GetBody().healthComponent.health / 2
                };
                transform.GetBody().healthComponent.TakeDamage(di);
            } // if
        } // Update
    } // ReaperTalons
} // ReaperTalons
