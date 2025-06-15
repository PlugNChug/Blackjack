using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.Localization;

namespace Blackjack.Common.UI
{
    // Simple UIElement wrapper around Terraria.UI.ItemSlot logic.
    public class BetItemSlot : UIElement
    {
        internal Item item;
        private readonly int context;
        private bool interactable = true;
        private static readonly HashSet<int> ValidBarItems = new();
        private static bool barListInitialized;

        private bool rightMouseDownStage1 = true;
        private bool rightMouseDownStage2 = false;
        private int rightMouseDownTimer = -1;

        // Build the set of valid bar items on demand, after all mods finish loading
        private static bool EnsureValidItem(Item item)
        {
            if (item.maxStack > 1) return true;
            return false;
        }

        Asset<Texture2D> itemSlotTexture = ModContent.Request<Texture2D>($"Blackjack/Assets/CustomItemSlot");
        Asset<Texture2D> emptySlotTexture = ModContent.Request<Texture2D>($"Blackjack/Assets/CustomItemSlotEmpty");

        public BetItemSlot(Item boundItem, int context = ItemSlot.Context.CreativeSacrifice, float size = 88f)
        {
            item = boundItem;
            this.context = context;
            Width.Set(size, 0f);
            Height.Set(size, 0f);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }

            // Right mouse hold handler
            if (!rightMouseDownStage1 && rightMouseDownTimer == -1)
            {
                // 30 frames
                rightMouseDownTimer = 30;
            }
            else if (rightMouseDownTimer > 0)
            {
                rightMouseDownTimer--;
            }
            else
            {
                rightMouseDownTimer = -1;
                rightMouseDownStage2 = true;
            }

        }

        public void DisableInteract()
        {
            interactable = false;
        }

        public void EnableInteract()
        {
            interactable = true;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);

            if (interactable)
            {
                // Check if the item is a currency or bar
                if (Main.mouseItem.IsCurrency || EnsureValidItem(Main.mouseItem) || Main.mouseItem.IsAir)
                {
                    Item temp = item.Clone();
                    item = Main.mouseItem.Clone();
                    Main.mouseItem = temp;
                    if (!Main.mouseItem.IsAir || !item.IsAir)
                        SoundEngine.PlaySound(SoundID.Grab);
                }
            }
        }

        public override void RightMouseDown(UIMouseEvent evt)
        {
            base.RightMouseDown(evt);

            if (interactable && (rightMouseDownStage1 || rightMouseDownStage2))
            {
                // Holding right click will accelerate the process of taking an item.
                if (rightMouseDownStage1)
                {
                    // Changing this to false will trigger a timer in Update()
                    rightMouseDownStage1 = false;
                }

                // Right clicking an item with an empty mouse will take one of that item.
                if (!item.IsAir)
                {
                    if (Main.mouseItem.IsAir)
                    {
                        Main.mouseItem = item.Clone();
                        Main.mouseItem.stack = 1;
                        item.stack -= 1;
                    }
                    else if (Main.mouseItem.type == item.type && Main.mouseItem.stack < Main.mouseItem.maxStack)
                    {
                        Main.mouseItem.stack += 1;
                        item.stack -= 1;
                    }
                }
            }
        }

        public override void RightMouseUp(UIMouseEvent evt)
        {
            base.RightMouseUp(evt);

            rightMouseDownStage1 = true;
            rightMouseDownStage2 = false;
            rightMouseDownTimer = -1;
        }

        // Draw the slot with built-in ItemSlot logic
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dims = GetDimensions();
            if (interactable)
            {
                spriteBatch.Draw(itemSlotTexture.Value, new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height), Color.White);
            }
            else
            {
                spriteBatch.Draw(itemSlotTexture.Value, new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height), Color.Gray);
            }
            if (!item.IsAir)
            {
                // Get the texture for the item
                Main.instance.LoadItem(item.type);
                Texture2D itemTexture = TextureAssets.Item[item.type].Value;

                Rectangle sourceRect = itemTexture.Bounds;
                if (Main.itemAnimations[item.type] != null)
                {
                    var anim = Main.itemAnimations[item.type];
                    int frameCount = anim.FrameCount;
                    int ticksPerFrame = anim.TicksPerFrame;
                    int frame = (int)(Main.GameUpdateCount / (uint)ticksPerFrame % frameCount);
                    int frameHeight = itemTexture.Height / frameCount;
                    sourceRect = new Rectangle(0, frameHeight * frame, itemTexture.Width, frameHeight);
                }

                // Calculate the scale (prevents distortion)
                float targetWidth = dims.Width / 2f;
                float targetHeight = dims.Height / 2f;
                float scale = Math.Min(targetWidth / sourceRect.Width, targetHeight / sourceRect.Height);

                int drawWidth = (int)(sourceRect.Width * scale);
                int drawHeight = (int)(sourceRect.Height * scale);
                Rectangle drawRect = new Rectangle((int)(dims.X + dims.Width / 2f - drawWidth / 2f), (int)(dims.Y + dims.Height / 2f - drawHeight / 2f), drawWidth, drawHeight);

                // Draw the item with PointClamp sampling (prevents blurring)
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
                spriteBatch.Draw(itemTexture, drawRect, sourceRect, Color.White);
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

                // Draw the item count
                DynamicSpriteFont font = FontAssets.ItemStack.Value;
                string stack = item.stack.ToString();
                spriteBatch.DrawString(font, stack, new Vector2(dims.X + dims.Width / 2 - font.MeasureString(stack).X / 2, dims.Y + dims.Height - 30f), Color.White);
            }
            else
            {
                spriteBatch.Draw(emptySlotTexture.Value, new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height), Color.White);
            }

            if (IsMouseHovering && item.IsAir)
            {
                Main.hoverItemName = Language.GetTextValue("Mods.Blackjack.UI.EmptyBetSlot");
            }
        }
    }
}
