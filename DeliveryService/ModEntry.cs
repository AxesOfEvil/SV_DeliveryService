using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;

using DeliveryService.Framework;

using SObject = StardewValley.Object;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;
using DeliveryService.Menus.Overlays;
using Pathoschild.Stardew.Common;

namespace DeliveryService
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private ModConfig Config;

        /// <summary>The overlay for the current menu which which lets the player navigate and edit chests (or <c>null</c> if not applicable).</summary>
        private DeliveryOverlay CurrentOverlay;
        private Dictionary<Chest, DeliveryChest> DeliveryChests = new Dictionary<Chest, DeliveryChest>();
        private long HostID = 0;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.Config = this.Helper.ReadConfig<ModConfig>();
            //helper.Events.GameLoop.DayEnding += this.OnDayEnding;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            //helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Display.MenuChanged += this.OnMenuChanged;
            helper.Events.GameLoop.Saving += this.Save;
            helper.Events.GameLoop.SaveLoaded += this.Load;
            helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        }
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            if (Config.WaitForWizardShop)
                Helper.Content.AssetEditors.Add(new WizardMail());
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (!DeliveryEnabled())
                return;
            // remove old overlay
            if (this.CurrentOverlay != null)
            {
                this.CurrentOverlay?.Dispose();
                this.CurrentOverlay = null;
            }
            this.Monitor.Log("New menu: " + e.NewMenu?.GetType(), LogLevel.Trace);
            if (e.NewMenu is ItemGrabMenu igm && igm.context is Chest container)
            {
                DeliveryChest chest;
                if (!this.DeliveryChests.TryGetValue(container, out chest))
                {
                    chest = new DeliveryChest(container);
                    DeliveryChests[container] = chest;
                    Monitor.Log($"Creating DeliveryChest {chest.Location}@{chest.TileLocation}", LogLevel.Trace);
                }
                this.Monitor.Log($"Applying DeliveryOverlay to {chest.Location}@{chest.TileLocation}", LogLevel.Trace);
                this.Monitor.Log($"Send: {string.Join(", ", chest.DeliveryOptions.Send)} Receive: {string.Join(", ", chest.DeliveryOptions.Receive)}", LogLevel.Trace);
                this.CurrentOverlay = new DeliveryOverlay(this.Monitor, igm, chest, this.Helper, this.ModManifest.UniqueID, this.HostID);
            }
        }
        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            if (!Context.IsWorldReady || !Context.IsMainPlayer)
                return;
            if (!DeliveryEnabled())
            {
                Monitor.Log("Delivery is not yet enabled", LogLevel.Info);
                return;
            }
            //this.Monitor.Log($"{Game1.player.Name} game-day ended {e}.", LogLevel.Trace);
            this.DoDelivery();
        }
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady || !Context.IsMainPlayer)
                return;
            if (!DeliveryEnabled())
            {
                Monitor.Log("Delivery is not yet enabled", LogLevel.Info);
                return;
            }
            // print button presses to the console window
            if (e.Button == Config.DeliverKey)
            {
                this.Monitor.Log($"--{Game1.player.Name} pressed {e.Button}.", LogLevel.Debug);
                this.DoDelivery();
            }
        }
        private void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != this.ModManifest.UniqueID)
                return;
            if (e.Type == "UpdateDeliveryOptions")
            {
                SyncDataModel message = e.ReadAs<SyncDataModel>();
                DeliveryChest dchest = GetDeliveryChestFromMessage(message);
                if (dchest != null)
                {
                    dchest.DeliveryOptions.Set(message.DeliveryOptions);
                    if (this.CurrentOverlay != null)
                        this.CurrentOverlay.ResetEdit();
                }
            }
            else if (e.Type == "RequestDeliveryOptions")
            {
                BaseDataModel message = e.ReadAs<BaseDataModel>();
                DeliveryChest dchest = GetDeliveryChestFromMessage(message);
                if (dchest != null)
                {
                    Helper.Multiplayer.SendMessage(new SyncDataModel(dchest), "UpdateDeliveryOptions", modIDs: new[] { e.FromModID }, playerIDs: new[] { e.FromPlayerID });
                }
            }
        }
        private void DoDelivery()
        {
            List<DeliveryChest> toChests = new List<DeliveryChest>();
            List<DeliveryChest> fromChests = new List<DeliveryChest>();
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;
            Chest[] containers = DeliveryChests.Keys.ToArray();
            foreach (Chest container in containers)
            {
                DeliveryChest chest = DeliveryChests[container];
                Monitor.Log($"chest:{chest} container:{container}, Chest:{chest.Chest} location:{chest.Location}", LogLevel.Trace);
                if (chest == null || !chest.Exists())
                {
                    this.Monitor.Log($"Chest {chest.Location}@{chest.TileLocation} no longer exists", LogLevel.Trace);
                    DeliveryChests.Remove(container);
                    continue;
                }
                if (chest.DeliveryOptions.Send.Contains(true))
                {
                    fromChests.Add(chest);
                }
                if (chest.DeliveryOptions.Receive.Contains(true))
                {
                    toChests.Add(chest);
                }
            }
            foreach (DeliveryChest fromChest in fromChests)
            {
                foreach (DeliveryChest toChest in toChests)
                {
                    List<DeliveryCategories> categories = new List<DeliveryCategories>();
                    if (fromChest == toChest)
                    {
                        continue;
                    }
                    foreach (DeliveryCategories category in Enum.GetValues(typeof(DeliveryCategories)))
                    {
                        if (fromChest.DeliveryOptions.Send[(int)category] && toChest.DeliveryOptions.Receive[(int)category])
                        {
                            categories.Add(category);
                        }
                    }
                    if (categories.Count == 0 || (fromChest.DeliveryOptions.MatchColor && fromChest.Chest.playerChoiceColor != toChest.Chest.playerChoiceColor))
                    {
                        continue;
                    }
                    this.Monitor.Log($"Moving {string.Join(", ", categories)} items from {fromChest.Location}@{fromChest.TileLocation} -> {toChest.Location}@{toChest.TileLocation}", LogLevel.Trace);
                    MoveItems(
                        from: fromChest.Chest,
                        to: toChest.Chest,
                        filter: categories.ToArray());
                }
            }
        }
        private void Save(object sender, SavingEventArgs e)
        {
            if (DeliveryEnabled() && Config.WaitForWizardShop)
            {
                Game1.addMailForTomorrow("DeliveryServiceWizardMail");
            }
            if (!Context.IsMainPlayer)
                return;
            List<SaveDataModel> save = new List<SaveDataModel>();
            foreach (DeliveryChest chest in this.DeliveryChests.Values)
            {
                if (chest == null || !chest.Exists())
                    continue;
                SaveDataModel data = new SaveDataModel(chest);
                if (data.Send.Count > 0 || data.Receive.Count > 0)
                    save.Add(data);
            }
            Helper.Data.WriteSaveData("delivery-service", save);
        }
        private void Load(object sender, SaveLoadedEventArgs e)
        {
            CurrentOverlay = null;
            DeliveryChests = new Dictionary<Chest, DeliveryChest>();
            HostID = 0;
            if (DeliveryEnabled() && Config.WaitForWizardShop)
            {
                Game1.addMailForTomorrow("DeliveryServiceWizardMail");
            }

            if (!Context.IsMainPlayer)
            {
                // Farmhands don't maintain state
                foreach (IMultiplayerPeer peer in this.Helper.Multiplayer.GetConnectedPlayers())
                {
                    if (peer.HasSmapi && peer.IsHost)
                    {
                        HostID = peer.PlayerID;
                        break;
                    }
                }
                return;
            }
            List<SaveDataModel> save = Helper.Data.ReadSaveData<List<SaveDataModel>>("delivery-service");
            if (save == null)
                return;
            foreach (SaveDataModel data in save)
            {
                DeliveryChest dchest = GetDeliveryChestFromMessage(data);
                if (dchest != null) {
                    bool[] send = new bool[Enum.GetValues(typeof(DeliveryCategories)).Length];
                    bool[] receive = new bool[Enum.GetValues(typeof(DeliveryCategories)).Length];
                    foreach (DeliveryCategories cat in Enum.GetValues(typeof(DeliveryCategories)))
                    {
                        if (data.Send.Contains(cat.ToString()))
                            send[(int)cat] = true;
                        if (data.Receive.Contains(cat.ToString()))
                            receive[(int)cat] = true;
                    }
                    dchest.DeliveryOptions.Set(send, receive, data.MatchColor);
                }
            }
        }
        private void MoveItems(Chest from, Chest to, DeliveryCategories[] filter)
        {
            // Store items because removing items aborts foreach()
            Item[] items = from.items.ToArray();
            foreach (Item item in items)
            {
                string type = "";
                if (item is SObject obj)
                {
                    type = obj.Type;
                }
                DeliveryCategories cat = item.getDeliveryCategory();
                this.Monitor.Log($"Found existing item {item.Name} Type: {type} Category: {item.getCategoryName()} cat: {cat.Name()}", LogLevel.Trace);
                if (!filter.Contains(cat))
                {
                    continue;
                }
                to.addItem(item);
                //this.Monitor.Log($"Removing item", LogLevel.Trace);
                from.items.Remove(item);
            }
        }
        private DeliveryChest GetDeliveryChestFromMessage(BaseDataModel message)
        {
            foreach (GameLocation location in LocationHelper.GetAccessibleLocations())
            {
                if (location.Name == message.Location)
                {
                    Item item;
                    if (message.isFridge)
                        if (location is FarmHouse house && Game1.player.HouseUpgradeLevel > 0)
                            item = house.fridge.Value;
                        else
                            break;
                    else
                        item = location.getObjectAtTile(message.X, message.Y);
                    if (item != null && item is Chest chest)
                    {
                        DeliveryChest dchest;
                        if (DeliveryChests.TryGetValue(chest, out dchest))
                        {
                            return dchest;
                        }
                        dchest = new DeliveryChest(chest);
                        DeliveryChests[chest] = dchest;
                        return dchest;
                    }
                }
            }
            return null;
        }
        private bool DeliveryEnabled()
        {
            return (!Config.WaitForWizardShop || Game1.player.hasMagicInk);
        }
    }
}