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
        internal Checkbox[] Checkbox { get; }
        internal Rectangle selectAll;
        internal Category(DeliveryCategories type, bool value=false) {
            this.Type = type;
            this.selectAll = new Rectangle(0, 0, 24, 24);
            this.Checkbox = new Checkbox[4];
            for (int i = 0; i < this.Checkbox.Length; i++)
            {
                this.Checkbox[i] = new Checkbox();
                this.Checkbox[i].Value = value;
            }
        }
        internal bool Any()
        {
            return this.Checkbox.Any(i => i.Value);
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
        private ClickableTextureComponent[] SendStars = new ClickableTextureComponent[5];
        private ClickableTextureComponent[] ReceiveStars = new ClickableTextureComponent[5];
        private ScrollBar scrollbar;
        private Checkbox MatchColor;
        private Checkbox PickupAll = new Checkbox();
        private Checkbox DropoffAll = new Checkbox();
        private Rectangle AllArrow = new Rectangle(365, 494, 12, 12);
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
        int padding;
        int maxLabelWidth;
        int LabelHeight;
        string hoverText;

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
            this.LabelHeight = (int)Game1.smallFont.MeasureString("ABC").Y + 2;

            this.scrollbar = new ScrollBar(helper.Events, helper.Input,
                new Rectangle(
                    this.bounds.X + 10 * Game1.pixelZoom,
                    this.bounds.Y + 10 * Game1.pixelZoom + 3 * LabelHeight,
                    this.bounds.Width - 20 * Game1.pixelZoom,
                    this.bounds.Height - 20 * Game1.pixelZoom - 5 * LabelHeight),
                this.SendCategories.Count,
                LabelHeight);
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
                this.drawDeliveryConfig(batch);
            }
            // cursor
            if (hoverText != null)
                DrawSimpleTooltip(batch, hoverText, Game1.smallFont);

            this.DrawCursor();
        }
        private void drawDeliveryConfig(SpriteBatch batch)
        {
            SpriteFont font = Game1.smallFont;
            //const int gutter = 10;
            int checkbox_width = 26;
            float topOffset = padding + LabelHeight;
            int padding2 = padding + 2 * maxLabelWidth + 20;

            batch.DrawMenuBackground(new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height));

            this.DrawAndPositionCheckbox(batch, this.MatchColor, bounds.X + padding, bounds.Y + padding, font, "Only deliver to chests of same color");

            batch.DrawString(font, "Pickup", new Vector2(bounds.X + padding + maxLabelWidth + 20, bounds.Y + (int)topOffset), Color.Black);
            batch.DrawString(font, "Drop-Off", new Vector2(bounds.X + padding + maxLabelWidth + 40 + 5 * checkbox_width, bounds.Y + (int)topOffset), Color.Black);
            topOffset += LabelHeight;
            batch.DrawString(font, "Category", new Vector2(bounds.X + padding, bounds.Y + (int)topOffset), Color.Black);
            Color[] color = new Color[5] { Color.White, Color.Black, Color.Silver, Color.Gold, Color.FromNonPremultiplied(136, 66, 237, 255) };
            for (int i = 0; i < 5; i++)
            {
                this.SendStars[i].draw(batch, color[i], 1f);
                this.ReceiveStars[i].draw(batch, color[i], 1f);
            }

            //batch.DrawString(font, "Drop-off", new Vector2(bounds.X + padding2, bounds.Y + (int)topOffset), Color.Black);
            //this.DrawAndPositionCheckbox(batch, font, DropoffAll, bounds.X + padding2 + (int)font.MeasureString("Drop-off").X + 10, bounds.Y + (int)topOffset, "All");
            topOffset = scrollbar.Coords.Y;
            int firstRow = this.scrollbar.CurrentItemIndex;
            int lastRow = Math.Min(this.SendCategories.Count, this.scrollbar.CurrentItemIndex + this.scrollbar.ItemsonScreen);
            for (int i = this.scrollbar.CurrentItemIndex; i < lastRow; i++)
            {
                Category send = this.SendCategories[i];
                Category receive = this.ReceiveCategories[i];
                batch.DrawString(font, send.Name(), new Vector2(bounds.X + padding, bounds.Y + (int)topOffset), Color.Black);
                send.selectAll.X = bounds.X + padding + maxLabelWidth + 20;
                receive.selectAll.X = send.selectAll.X + 20 + 5 * checkbox_width;
                receive.selectAll.Y = send.selectAll.Y = bounds.Y + (int)topOffset;
                batch.Draw(Sprites.Icons.Sheet, new Vector2(send.selectAll.X, send.selectAll.Y), AllArrow, Color.White, 0, Vector2.Zero, (float)send.selectAll.Width / AllArrow.Width, SpriteEffects.None, 1f);
                batch.Draw(Sprites.Icons.Sheet, new Vector2(receive.selectAll.X, receive.selectAll.Y), AllArrow, Color.White, 0, Vector2.Zero, (float)receive.selectAll.Width / AllArrow.Width, SpriteEffects.None, 1f);
                for (int j = 0; j < 4; j++)
                {
                    this.DrawAndPositionCheckbox(batch, send.Checkbox[j], bounds.X + padding + maxLabelWidth + 20 + (1+ j) * checkbox_width, bounds.Y + (int)topOffset);
                    this.DrawAndPositionCheckbox(batch, receive.Checkbox[j], bounds.X + padding + maxLabelWidth + 40 + (6 + j) * checkbox_width, bounds.Y + (int)topOffset);
                }
                topOffset += LabelHeight;
            }
            this.DrawButton(batch, Game1.smallFont, this.EditSaveButtonArea, bounds.X + padding, bounds.Y + (int)topOffset, "Save", Color.DarkGreen, out Rectangle saveButtonBounds);
            this.EditExitButton.draw(batch);
            this.scrollbar.Draw(batch);
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
            SpriteFont font = Game1.smallFont;
            this.padding = Game1.pixelZoom * 10;
            this.maxLabelWidth = 24 + 10 + (int)this.SendCategories.Select(p => font.MeasureString(p.Name()).X).Max();
            this.LabelHeight = (int)font.MeasureString("ABC").Y + 2;
            Rectangle sprite = Sprites.Icons.Junimo;
            int topOffset = -Game1.pixelZoom * 12;
            int leftOffset = Game1.pixelZoom * 16;
            float zoom = 2f * Game1.pixelZoom / 2f;
            int checkbox_width = 26;

            Rectangle buttonBounds = new Rectangle(this.Menu.xPositionOnScreen + leftOffset + this.Menu.width - (int)(sprite.Width * zoom), this.Menu.yPositionOnScreen + topOffset, (int)(sprite.Width * zoom), (int)(sprite.Height * zoom));
            this.EditButton = new ClickableTextureComponent("edit-delivery", buttonBounds, "junimo", "junimo", Sprites.Icons.Sheet, sprite, zoom);
            this.EditExitButton = new ClickableTextureComponent(new Rectangle(bounds.Right - 9 * Game1.pixelZoom, bounds.Y - Game1.pixelZoom * 2, Sprites.Icons.ExitButton.Width * Game1.pixelZoom, Sprites.Icons.ExitButton.Height * Game1.pixelZoom), Sprites.Icons.Sheet, Sprites.Icons.ExitButton, Game1.pixelZoom);
            int LabelY = padding + 2 * LabelHeight;
            Rectangle[] sprites = new Rectangle[5]
            {
                AllArrow,
                Sprites.Icons.EmptyCheckbox,
                new Rectangle(202, 374, 8, 8),
                new Rectangle(202, 374, 8, 8),
                new Rectangle(202, 374, 8, 8)
            };
            string[] hover = new string[5] { "Any", "Regular", "Silver", "Gold", "Iridium"};
            for (int i = 0; i < 5; i++)
            {
                this.SendStars[i] = new ClickableTextureComponent(
                    "star",
                    new Rectangle(bounds.X + padding + maxLabelWidth + 20 + i * checkbox_width, bounds.Y + LabelY, 22, 22),
                    null, hover[i], Sprites.Icons.Sheet, sprites[i], (checkbox_width -2) / (float)sprites[i].Width);
                this.ReceiveStars[i] = new ClickableTextureComponent(
                    "star",
                    new Rectangle(bounds.X + padding + maxLabelWidth + 40 + (5 + i) * checkbox_width, bounds.Y + LabelY, 22, 22),
                    null, hover[i], Sprites.Icons.Sheet, sprites[i], (checkbox_width - 2) / (float)sprites[i].Width);
            }
        }
        protected void SaveEdit()
        {
            bool[] send = new bool[this.SendCategories.Count];
            bool[] receive = new bool[this.ReceiveCategories.Count];
            for (int i = 0; i < this.SendCategories.Count; i++)
            {
                send[i] = this.SendCategories[i].Checkbox[0].Value;
                receive[i] = this.ReceiveCategories[i].Checkbox[0].Value;
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
                this.hasDelivery = false;
                for (int j = 0; j < this.SendCategories[i].Checkbox.Length; j++)
                {
                    this.SendCategories[i].Checkbox[j].Value = this.Chest.DeliveryOptions.Send[i];
                    this.ReceiveCategories[i].Checkbox[j].Value = this.Chest.DeliveryOptions.Receive[i];
                    if (this.Chest.DeliveryOptions.Send[i] || this.Chest.DeliveryOptions.Send[i])
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
            this.Monitor.Log($"Clicked @{x},{y}", LogLevel.Debug);
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
                    this.scrollbar.Hide();
                    this.isEditing = false;
                    return true;
                }
                else if (this.EditExitButton.containsPoint(x, y))
                {
                    this.scrollbar.Hide();
                    this.isEditing = false;
                    ResetEdit();
                    return true;
                }
                else if (this.MatchColor.GetBounds().Contains(x,y))
                {
                    this.MatchColor.Toggle();
                }
                for (int i = 0; i < 5; i++)
                {
                    if (this.SendStars[i].containsPoint(x, y))
                    {
                        SetAllCategoryBoxes(SendCategories, ReceiveCategories, i);
                        return true;
                    }
                    if (this.ReceiveStars[i].containsPoint(x, y))
                    {
                        SetAllCategoryBoxes(ReceiveCategories, SendCategories, i);
                        return true;
                    }
                }
                int firstRow = this.scrollbar.CurrentItemIndex;
                int lastRow = Math.Min(this.SendCategories.Count, this.scrollbar.CurrentItemIndex + this.scrollbar.ItemsonScreen);
                for (int i = firstRow; i < lastRow; i++) {
                    if (this.SendCategories[i].selectAll.Contains(x, y)) {
                        bool value = !SendCategories[i].Checkbox.All(x1 => x1.Value);
                        SetSingleCategoryBox(SendCategories[i], ReceiveCategories[i], 15, value);
                        return true;
                    }
                    if (this.ReceiveCategories[i].selectAll.Contains(x, y))
                    {
                        bool value = !ReceiveCategories[i].Checkbox.All(x1 => x1.Value);
                        SetSingleCategoryBox(ReceiveCategories[i], SendCategories[i], 15, value);
                        return true;
                    }
                    for (int j = 0; j < 4; j++) {
                        if (SendCategories[i].Checkbox[j].GetBounds().Contains(x, y))
                        {
                            SetSingleCategoryBox(SendCategories[i], ReceiveCategories[i], 1 << j, !SendCategories[i].Checkbox[j].Value);
                            return true;
                        }
                        if (ReceiveCategories[i].Checkbox[j].GetBounds().Contains(x, y))
                        {
                            SetSingleCategoryBox(ReceiveCategories[i], SendCategories[i], 1 << j, !ReceiveCategories[i].Checkbox[j].Value);
                            return true;
                        }
                    }
                }
                /*
                 *else if (this.PickupAll.GetBounds().Contains(x, y) || this.DropoffAll.GetBounds().Contains(x, y)) {
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
                  */
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
            if (!this.isDrawn)
                return false;
            hoverText = null;
            if (this.isEditing)
            {
                this.EditExitButton.tryHover(x, y);
                for (int i = 0; i < 5; i++)
                {
                    if (this.SendStars[i].containsPoint(x, y))
                        hoverText = this.SendStars[i].hoverText;
                    this.SendStars[i].tryHover(x, y);
                    this.ReceiveStars[i].tryHover(x, y);
                }
            } else
            {
                this.EditButton.tryHover(x, y);
                return false;
            }
            return true;
        }
        protected void OpenEditMenu()
        {
            isEditing = true;
            this.scrollbar.Show();
        }
        void SetAllCategoryBoxes(List<Category> primary, List<Category> secondary, int idx)
        {
            int mask = idx == 0 ? 15 : (1 << (idx - 1));
            bool value = idx == 0
                ? !primary.All(x1 => x1.Checkbox.All(x2 => x2.Value))
                : !primary.All(x1 => x1.Checkbox[idx - 1].Value);

            for (int j = 0; j < this.SendCategories.Count; j++)
                SetSingleCategoryBox(primary[j], secondary[j], mask, value);
        }
        void SetSingleCategoryBox(Category primary, Category secondary, int mask, bool value)
        {
            for (int i = 0; i < 4; i++)
            {
                if ((mask & (1 << i)) == 0)
                    continue;
                primary.Checkbox[i].Value = value;
                if (value)
                    secondary.Checkbox[i].Value = false;
            }
        }
        /// <summary>Draw a checkbox to the screen, including any position updates needed.</summary>
        /// <param name="batch">The sprite batch being drawn.</param>
        /// <param name="font">The font to use for the checkbox label.</param>
        /// <param name="checkbox">The checkbox to draw.</param>
        /// <param name="x">The top-left X position to start drawing from.</param>
        /// <param name="y">The top-left Y position to start drawing from.</param>
        /// <param name="textKey">The translation key for the checkbox label.</param>
        private void DrawAndPositionCheckbox(SpriteBatch batch, Checkbox checkbox, int x, int y, SpriteFont font = null, string textKey = null)
        {
            checkbox.X = x;
            checkbox.Y = y;
            checkbox.Width = 24;
            checkbox.Draw(batch);
            if (textKey != null && font != null)
            {
                string label = textKey;
                batch.DrawString(font, label, new Vector2(x + 7 + checkbox.Width, y), checkbox.Value ? Color.Red : Color.Black);
            }
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
        private void DrawSimpleTooltip(SpriteBatch b, string hoverText, SpriteFont font)
        {
            Vector2 textSize = font.MeasureString(hoverText);
            int width = (int)textSize.X + Game1.tileSize / 2;
            int height = Math.Max(60, (int)textSize.Y + Game1.tileSize / 2);
            int x = Game1.getOldMouseX() + Game1.tileSize / 2;
            int y = Game1.getOldMouseY() - Game1.tileSize / 2;
            if (x + width > Game1.viewport.Width)
            {
                x = Game1.viewport.Width - width;
                y += Game1.tileSize / 4;
            }
            if (y + height < 0)
            {
                x += Game1.tileSize / 4;
                y = Game1.viewport.Height + height;
            }
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White);
            if (hoverText.Length > 1)
            {
                Vector2 tPosVector = new Vector2(x + (Game1.tileSize / 4), y + (Game1.tileSize / 4 + 4));
                b.DrawString(font, hoverText, tPosVector + new Vector2(2f, 2f), Game1.textShadowColor, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                b.DrawString(font, hoverText, tPosVector + new Vector2(0f, 2f), Game1.textShadowColor, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                b.DrawString(font, hoverText, tPosVector + new Vector2(2f, 0f), Game1.textShadowColor, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                b.DrawString(font, hoverText, tPosVector, Game1.textColor * 0.9f, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
            }
        }

    }
}
