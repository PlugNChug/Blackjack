using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace Blackjack.Common.UI
{
        // UI hover image button class from ExampleMod
        internal class UIHoverImageButton : UIImageButton
        {
                // Key used for localized tooltip text shown on hover
                internal string hoverTextKey;

                public UIHoverImageButton(Asset<Texture2D> texture, string hoverTextKey) : base(texture) {
                        this.hoverTextKey = hoverTextKey;
                }

                protected override void DrawSelf(SpriteBatch spriteBatch) {
                        // When you override UIElement methods, don't forget call the base method
                        // This helps to keep the basic behavior of the UIElement
                        base.DrawSelf(spriteBatch);

                        // IsMouseHovering becomes true when the mouse hovers over the current UIElement
                        if (IsMouseHovering)
                                Main.hoverItemName = Language.GetTextValue(hoverTextKey);
                }
        }
}
