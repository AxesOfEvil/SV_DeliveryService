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

namespace DeliveryService
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private ModConfig Config;
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
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            //this.Monitor.Log($"{Game1.player.Name} game-day ended {e}.", LogLevel.Debug);
            foreach (ItemPair pair in this.Config.DeliveryMap)
            {
                this.DoDelivery(pair.GetFromId(), pair.GetToId());
            }
        }
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            // print button presses to the console window
            if (e.Button == Keys.PrintScreen.ToSButton())
            {
                this.Monitor.Log($"--{Game1.player.Name} pressed {e.Button}.", LogLevel.Info);
                foreach (ItemPair pair in this.Config.DeliveryMap)
                {
                    this.DoDelivery(pair.GetFromId(), pair.GetToId());
                }
            }
        }

        private void DoDelivery(ItemDescriptor fromId, ItemDescriptor toId)
        {
            List<DeliveryChest> toChests = new List<DeliveryChest>();
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;
            foreach (DeliverySign deliverysign in GetDeliverySigns(toId))
            {
                foreach (DeliveryChest chest in GetDeliveryChests(deliverysign))
                {
                    this.Monitor.Log($"Destination Chest color: {chest.Chest.playerChoiceColor}");
                    toChests.Add(chest);
                }
            }
            if (toChests.Count == 0)
            {
                this.Monitor.Log($"No destination chests found", LogLevel.Debug);
                return;
            }
            foreach (DeliverySign deliverysign in GetDeliverySigns(fromId))
            {
                GameLocation location = deliverysign.Location;
                foreach (DeliveryChest chest in GetDeliveryChests(deliverysign))
                {
                    //this.Monitor.Log($"Source Chest color: {chest.Chest.playerChoiceColor}", LogLevel.Debug);
                    foreach (DeliveryChest dest in GetChestByColor(toChests, chest.Chest.playerChoiceColor))
                    {
                        if (dest.Chest == chest.Chest)
                        {
                            continue;
                        }
                        this.Monitor.Log($"Moving items from {chest.Sign.Location}@{chest.Chest.TileLocation} -> {dest.Sign.Location}@{dest.Chest.TileLocation}", LogLevel.Info);
                        MoveItems(
                            from: chest.Chest,
                            to: dest.Chest);
                        break;
                    }
                }
            }
        }
        private IEnumerable<DeliveryChest> GetChestByColor(List<DeliveryChest> chests, Netcode.NetColor color)
        {
            foreach(DeliveryChest chest in chests)
            {
                if (chest.Chest.playerChoiceColor == color)
                {
                    yield return chest;
                }
            }
        }
        private void MoveItems(Chest from, Chest to)
        {
            // Store items because removing items aborts foreach()
            Item[] items = from.items.ToArray();
            foreach (Item item in items)
            {
                //this.Monitor.Log($"Moving {item}", LogLevel.Debug);
                /*
                Item dest_item = FindItemInChest(to, item);
                if (dest_item != null) {
                    this.Monitor.Log($"Found existing item {item} count: {item.Stack}", LogLevel.Debug);
                    int newStackSize = dest_item.Stack + item.Stack;
                    if (newStackSize > dest_item.maximumStackSize())
                    {
                        dest_item.Stack = dest_item.maximumStackSize();
                        item.Stack = newStackSize - dest_item.maximumStackSize();
                    } else
                    {
                        dest_item.Stack = newStackSize;
                        from.items.Remove(item);
                        continue;
                    }
                }
                this.Monitor.Log($"Adding new item {item} {item.Stack}", LogLevel.Debug);
                */
                to.addItem(item);
                //this.Monitor.Log($"Removing item", LogLevel.Debug);
                from.items.Remove(item);
            }
        }
        private Item FindItemInChest(Chest chest, Item item)
        {
            return null;
        }
        private IEnumerable<DeliveryChest> GetDeliveryChests(DeliverySign deliverysign)
        {
            GameLocation location = deliverysign.Location;
            Sign sign = deliverysign.Sign;
            Vector2 tile = sign.TileLocation;
            foreach (int x in Enumerable.Range(-1, 3))
            {
                foreach (int y in Enumerable.Range(-1, 3))
                {
                    if (x == 0 && y == 0) {
                        continue;
                    }
                    int tile_x = (int)tile.X + x;
                    int tile_y = (int)tile.Y + y;
                    if (tile_x < 0 || tile_y < 0)
                    {
                        continue;
                    }
                    //this.Monitor.Log($"Looking for chest at {location.Name}@({tile_x},{tile_y})", LogLevel.Debug);
                    SObject obj = location.getObjectAtTile((int)tile_x, (int)tile_y);
                    if (obj is Chest chest)
                    {
                        //this.Monitor.Log($"Found chest at {location.Name}@({tile_x},{tile_y})", LogLevel.Debug);
                        yield return new DeliveryChest(deliverysign, chest);
                    }
                }
            }
        }
        private IEnumerable<DeliverySign> GetDeliverySigns(ItemDescriptor displayId)
        {
            foreach (GameLocation location in GetLocations())
            {
                foreach (KeyValuePair<Vector2, SObject> pair in location.Objects.Pairs)
                {
                    Vector2 tile = pair.Key;
                    SObject obj = pair.Value;

                    // chests
                    if (obj is Sign sign)
                    {
                        Item item = sign.displayItem;
                        if (item is SObject objitem)
                        {
                            //this.Monitor.Log($"Found {location.Name}@({obj.TileLocation}) -- sign: {objitem.Type}/{objitem.Name}({objitem.DisplayName}-{objitem.ParentSheetIndex})", LogLevel.Debug);
                            if (! displayId.MatchItem(objitem)) 
                            {
                                continue;
                            }
                            //this.Monitor.Log($"Match!", LogLevel.Debug);
                            yield return new DeliverySign(
                                location: location,
                                sign: sign
                                );
                        }
                        else
                        {
                            this.Monitor.Log($"{location.Name} ({obj.TileLocation}) found sign {sign.displayItem}.", LogLevel.Debug);
                        }
                    }
                }
            }
        }
        // From Pathoschild.ChestsAnywhere
        /// <summary>Get the locations which are accessible to the current player (regardless of settings).</summary>
        private IEnumerable<GameLocation> GetAccessibleLocations()
        {
            // main player can access chests in any location
            if (Context.IsMainPlayer)
                return GetLocations();

            // secondary player can only safely access chests in their current location
            // (changes to other locations aren't synced to the other players)
            return new[] { Game1.player.currentLocation };
        }

        // From Pathoschild.Common
        public static IEnumerable<GameLocation> GetLocations()
        {
            return Game1.locations
                .Concat(
                    from location in Game1.locations.OfType<BuildableGameLocation>()
                    from building in location.buildings
                    where building.indoors.Value != null
                    select building.indoors.Value
                );
        }
    }
}