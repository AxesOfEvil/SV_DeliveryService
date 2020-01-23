using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeliveryService.Framework;
using DeliveryService.Menus.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.Common;
using Pathoschild.Stardew.Common.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace DeliveryService.Menus.Overlays
{
    public class Category
    {
        internal DeliveryCategories Type;
        internal Checkbox Checkbox { get; }
        internal Category(DeliveryCategories type, bool value=false) {
            this.Type = type;
            this.Checkbox = new Checkbox();
            this.Checkbox.Value = value;
        }
        internal string Name()
        {
            return this.Type.Name();
        }
        static public List<Category>CategoryList(bool[]values)
        {
            List<Category> categories = new List<Category>();
            foreach (DeliveryCategories type in Enum.GetValues(typeof(DeliveryCategories)))
            {
                categories.Add(new Category(type, values[(int)type]));
            }
            return categories;
        }
    }
    internal class DeliveryOverlay : BaseOverlay
    {
        ItemGrabMenu Menu;
        IMonitor Monitor;
        IMultiplayerHelper Multiplayer;
        /// <summary>The edit button.</summary>
        protected ClickableTextureComponent EditButton;
        /// <summary>The clickable area which saves the edit form.</summary>
        protected ClickableComponent EditSaveButtonArea;
        /// <summary>The top-right button which closes the edit form.</summary>
        private ClickableTextureComponent EditExitButton;
        private Checkbox MatchColor;
        private Checkbox PickupAll = new Checkbox();
        private Checkbox DropoffAll = new Checkbox();
        private Rectangle bounds;
        private string ModID;
        private long HostID;
        bool isDrawn;
        bool isEditing = false;
        bool hasDelivery;
        DeliveryChest Chest;
        int Count = 0;
        readonly List<Category> SendCategories;
        readonly List<Category> ReceiveCategories;

        /*********
        ** Accessors
        *********/

        /// <summary>The menu instance for which the overlay was created.</summary>
        public DeliveryOverlay(IMonitor monitor, ItemGrabMenu menu, DeliveryChest chest, IModHelper helper, string modid, long hostid)
            : base(helper.Events, helper.Input, keepAlive: () => Game1.activeClickableMenu is ItemGrabMenu)
        {
            this.Menu = menu;
            this.Chest = chest;
            this.Monitor = monitor;
            this.Multiplayer = helper.Multiplayer;
            this.ModID = modid;
            this.HostID = hostid;
            this.EditSaveButtonArea = new ClickableComponent(new Rectangle(0, 0, Game1.tileSize, Game1.tileSize), "save-delivery");
            this.SendCategories = Category.CategoryList(chest.DeliveryOptions.Send);
            this.ReceiveCategories = Category.CategoryList(chest.DeliveryOptions.Receive);
            this.MatchColor = new Checkbox(chest.DeliveryOptions.MatchColor);
            //this.bounds = new Rectangle(this.Menu.xPositionOnScreen, this.Menu.yPositionOnScreen, this.Menu.width, this.Menu.Height);
            this.bounds = new Rectangle(10, 10, Game1.viewport.Width - 20, Game1.viewport.Height - 20);
            if (! Context.IsMainPlayer)
                this.Multiplayer.SendMessage(new SerializableChestLocation(chest), "RequestDeliveryOptions", modIDs: new[] { this.ModID }, playerIDs: new[] { this.HostID });
        }
        protected override void Draw(SpriteBatch batch)
        {
            if (!this.isDrawn)
            {
                ReinitializeComponents();
                ResetEdit();
                this.isDrawn = true;
            }
            this.Count++;
            if (! isEditing)
            {
                this.EditButton.draw(batch, GetIconColor(), 1f);
            }
            else {
                SpriteFont font = Game1.smallFont;
                //const int gutter = 10;
                int padding = Game1.pixelZoom * 10;
                int maxLabelWidth = 24 + 10 + (int)this.SendCategories.Select(p => font.MeasureString(p.Name()).X).Max();
                int LabelHeight = (int)font.MeasureString("ABC").Y;
                float topOffset = padding + LabelHeight;
                int padding2 = padding + 2 * maxLabelWidth + 20;
                int numRows = (this.SendCategories.Count + 1) / 2;

                batch.DrawMenuBackground(new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height));

                this.DrawAndPositionCheckbox(batch, font, this.MatchColor, bounds.X + padding, bounds.Y + padding, "Only deliver to chests of same color");

                batch.DrawString(font, "Pickup", new Vector2(bounds.X + padding, bounds.Y + (int)topOffset), Color.Black);
                this.DrawAndPositionCheckbox(batch, font, PickupAll, bounds.X + padding + (int)font.MeasureString("Pickup").X + 10, bounds.Y + (int)topOffset, "All");

                batch.DrawString(font, "Drop-off", new Vector2(bounds.X + padding2, bounds.Y + (int)topOffset), Color.Black);
                this.DrawAndPositionCheckbox(batch, font, DropoffAll, bounds.X + padding2 + (int)font.MeasureString("Drop-off").X + 10, bounds.Y + (int)topOffset, "All");
                topOffset += LabelHeight;
                for (int i = 0; i < numRows; i++)
                {
                    int j = i + numRows;
                    Category send = this.SendCategories[i];
                    Category receive = this.ReceiveCategories[i];
                    this.DrawAndPositionCheckbox(batch, font, send.Checkbox, bounds.X + padding, bounds.Y + (int)topOffset, send.Name());
                    this.DrawAndPositionCheckbox(batch, font, receive.Checkbox, bounds.X + padding2, bounds.Y + (int)topOffset, receive.Name());
                    if (j < this.SendCategories.Count)
                    {
                        send = this.SendCategories[j];
                        receive = this.ReceiveCategories[j];
                        this.DrawAndPositionCheckbox(batch, font, send.Checkbox, bounds.X + padding + maxLabelWidth + 10, bounds.Y + (int)topOffset, send.Name());
                        this.DrawAndPositionCheckbox(batch, font, receive.Checkbox, bounds.X + padding2 + maxLabelWidth + 10, bounds.Y + (int)topOffset, receive.Name());
                    }
                    topOffset += LabelHeight;
                }
                this.DrawButton(batch, Game1.smallFont, this.EditSaveButtonArea, bounds.X + padding, bounds.Y + (int)topOffset, "Save", Color.DarkGreen, out Rectangle saveButtonBounds);
                this.EditExitButton.draw(batch);
            }
            // cursor
            this.DrawCursor();
        }
        protected Color GetIconColor()
        {
            int idx = this.Count % 200;
            int r, g, b;
            if (! this.hasDelivery)
            {
                return Color.LightGray;
            }
            if (idx < 100)
            {
                r = 255;
                g = 256 * idx / 100;
                b = 0;
            } else
            {
                r = 255;
                g = 256 * (200 - idx) / 100;
                b = 0;
            }
            return Color.FromNonPremultiplied(r, g, b, 255);
        }
        protected void ReinitializeComponents()
        {
            Rectangle sprite = Sprites.Icons.Junimo;
            int topOffset = -Game1.pixelZoom * 12;
            int leftOffset = Game1.pixelZoom * 16;
            float zoom = 2f * Game1.pixelZoom / 2f;

            Rectangle buttonBounds = new Rectangle(this.Menu.xPositionOnScreen + leftOffset + this.Menu.width - (int)(sprite.Width * zoom), this.Menu.yPositionOnScreen + topOffset, (int)(sprite.Width * zoom), (int)(sprite.Height * zoom));
            this.EditButton = new ClickableTextureComponent("edit-delivery", buttonBounds, null, null, Sprites.Icons.Sheet, sprite, zoom);
            this.EditExitButton = new ClickableTextureComponent(new Rectangle(bounds.Right - 9 * Game1.pixelZoom, bounds.Y - Game1.pixelZoom * 2, Sprites.Icons.ExitButton.Width * Game1.pixelZoom, Sprites.Icons.ExitButton.Height * Game1.pixelZoom), Sprites.Icons.Sheet, Sprites.Icons.ExitButton, Game1.pixelZoom);
        }
        protected void SaveEdit()
        {
            bool[] send = new bool[this.SendCategories.Count];
            bool[] receive = new bool[this.ReceiveCategories.Count];
            for (int i = 0; i < this.SendCategories.Count; i++)
            {
                send[i] = this.SendCategories[i].Checkbox.Value;
                receive[i] = this.ReceiveCategories[i].Checkbox.Value;
            }
            Monitor.Log($"Saving Categories {this.Chest.Location} Send:{string.Join(", ", send)}, Receive:{string.Join(", ", receive)}", LogLevel.Trace);
            this.Chest.DeliveryOptions.Set(send, receive, MatchColor.Value);
            SyncDataModel sync = new SyncDataModel(this.Chest);
            this.Multiplayer.SendMessage(sync, "UpdateDeliveryOptions", modIDs: new[] { this.ModID }); //, playerIDs: new[] { this.HostID });
        }
        public void ResetEdit()
        {
            this.hasDelivery = false;
            for (int i = 0; i < this.SendCategories.Count; i++)
            {
                this.SendCategories[i].Checkbox.Value = this.Chest.DeliveryOptions.Send[i];
                this.ReceiveCategories[i].Checkbox.Value = this.Chest.DeliveryOptions.Receive[i];
                if (this.Chest.DeliveryOptions.Send[i] || this.Chest.DeliveryOptions.Receive[i])
                {
                    this.hasDelivery = true;
                }
            }
            this.MatchColor.Value = this.Chest.DeliveryOptions.MatchColor;
        }
        /// <summary>Release all resources.</summary>
        public override void Dispose()
        {
            Monitor.Log($"Disposing {Chest.Location}", LogLevel.Trace);
            base.Dispose();
        }
        protected override void ReceiveGameWindowResized(xTile.Dimensions.Rectangle oldBounds, xTile.Dimensions.Rectangle newBounds)
        {
            this.ReinitializeComponents();
        }
        protected override bool ReceiveLeftClick(int x, int y)
        {
            if (!this.isDrawn)
            {
                return false;
            }
            if (!isEditing)
            {
                if (this.EditButton.containsPoint(x, y))
                {
                    OpenEditMenu();
                    return true;
                }
            } else
            {
                // save button
                if (this.EditSaveButtonArea.containsPoint(x, y))
                {
                    SaveEdit();
                    ResetEdit();
                    this.isEditing = false;
                    return true;
                }
                else if (this.EditExitButton.containsPoint(x, y))
                {
                    this.isEditing = false;
                    ResetEdit();
                    return true;
                }
                else if (this.MatchColor.GetBounds().Contains(x,y))
                {
                    this.MatchColor.Toggle();
                }
                else if (this.PickupAll.GetBounds().Contains(x, y) || this.DropoffAll.GetBounds().Contains(x, y)) {
                    bool pickup = this.PickupAll.GetBounds().Contains(x, y);
                    List<Category> primary = pickup ? this.SendCategories : this.ReceiveCategories;
                    List<Category> secondary = pickup ? this.ReceiveCategories : this.SendCategories;
                    bool all = primary.All(i => i.Checkbox.Value);
                    for (int i = 0; i < this.SendCategories.Count; i++)
                    {
                        if (! all)
                        {
                            primary[i].Checkbox.Value = true;
                            secondary[i].Checkbox.Value = false;
                        } else
                        {
                            primary[i].Checkbox.Value = false;
                        }
                    }
                }
                for (int i = 0; i < this.SendCategories.Count; i++)
                {
                    if (this.SendCategories[i].Checkbox.GetBounds().Contains(x, y))
                    {
                        this.SendCategories[i].Checkbox.Toggle();
                        if(this.SendCategories[i].Checkbox.Value)
                        {
                            this.ReceiveCategories[i].Checkbox.Value = false;
                        }
                        break;
                    }
                    if (this.ReceiveCategories[i].Checkbox.GetBounds().Contains(x, y))
                    {
                        this.ReceiveCategories[i].Checkbox.Toggle();
                        if (this.ReceiveCategories[i].Checkbox.Value)
                        {
                            this.SendCategories[i].Checkbox.Value = false;
                        }
                        break;
                    }
                }
                return true;
            }
            return false;
        }
        /// <summary>The method invoked when the cursor is hovered.</summary>
        /// <param name="x">The cursor's X position.</param>
        /// <param name="y">The cursor's Y position.</param>
        /// <returns>Whether the event has been handled and shouldn't be propagated further.</returns>
        protected override bool ReceiveCursorHover(int x, int y)
        {
            if (this.isEditing)
            {
                this.EditExitButton.tryHover(x, y);
            } else
            {
                //this.EditButton.tryHover(x, y);
            }
            return true;
        }
        protected void OpenEditMenu()
        {
            isEditing = true;
        }
        /// <summary>Draw a checkbox to the screen, including any position updates needed.</summary>
        /// <param name="batch">The sprite batch being drawn.</param>
        /// <param name="font">The font to use for the checkbox label.</param>
        /// <param name="checkbox">The checkbox to draw.</param>
        /// <param name="x">The top-left X position to start drawing from.</param>
        /// <param name="y">The top-left Y position to start drawing from.</param>
        /// <param name="textKey">The translation key for the checkbox label.</param>
        private Vector2 DrawAndPositionCheckbox(SpriteBatch batch, SpriteFont font, Checkbox checkbox, int x, int y, string textKey)
        {
            checkbox.X = x;
            checkbox.Y = y;
            checkbox.Width = 24;
            checkbox.Draw(batch);
            string label = textKey;
            batch.DrawString(font, label, new Vector2(x + 7 + checkbox.Width, y), checkbox.Value ? Color.Red : Color.Black);
            Vector2 labelSize = font.MeasureString(label);
            return new Vector2(checkbox.Width + 7 + checkbox.Width + labelSize.X, Math.Max(checkbox.Width, labelSize.Y));
        }
        private Vector2 DrawButton(SpriteBatch batch, SpriteFont font, ClickableComponent clickArea, int x, int y, string textKey, in Color color, out Rectangle bounds)
        {
            // get text
            string label = textKey;
            Vector2 labelSize = font.MeasureString(label);

            // draw button
            CommonHelper.DrawButton(batch, new Vector2(x, y), labelSize, out Vector2 contentPos, out bounds);
            Utility.drawBoldText(batch, label, font, contentPos, color);

            // align clickable area
            clickArea.bounds = bounds;

            // return size
            return new Vector2(bounds.Width, bounds.Height);
        }

    }
}
