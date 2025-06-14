using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Blackjack.Common.Config;

namespace Blackjack.Common.UI
{
    // Main UIState that houses the draggable panel and game logic.
    // This state is only loaded on the client and manages all
    // user interface elements for the blackjack mini-game.
    internal class BlackjackOverlayUIState : UIState
    {
        public DraggableUIPanel BlackjackPanel;
        private BlackjackGame blackjackGame;
        private BetItemSlot betItemSlot;

        private UIImage table;
        private UIHoverImageButton closeButton;
        private UIHoverImageButton closeButtonInactive;
        private UIHoverImageButton playButton;
        private UIHoverImageButton hitButton;
        private UIHoverImageButton standButton;

        private bool buttonsActive = false;         // The hit/stand buttons are active if a game is in progress and the player hasn't stood
        private bool playerStood = false;           // Has the player stood?
        private bool playButtonActive = false;      // The play button is active if no game is in progress and the player has placed a bet
        private bool closeButtonActive = true;     // The close button is active if no game is in progress

        float uiScale = ModContent.GetInstance<Appearance>().BlackjackUIScale;
        float boxWidth;
        float boxHeight;

        public override void OnInitialize()
        {
            boxWidth = 1280f * uiScale;
            boxHeight = 720f * uiScale;

            // Blackjack UI Panel
            BlackjackPanel = new DraggableUIPanel();
            BlackjackPanel.SetPadding(0);
            SetRectangle(BlackjackPanel, left: 0f, top: Main.screenHeight - boxHeight, width: boxWidth, height: boxHeight);

            // Background and decoration
            BlackjackPanel.BackgroundColor = Color.Transparent;
            BlackjackPanel.BorderColor = Color.Transparent;
            Asset<Texture2D> tableTexture = ModContent.Request<Texture2D>("Blackjack/Assets/Table");
            table = new UIImage(tableTexture);
            table.ScaleToFit = true;    // Crucial to make the image fit the panel
            SetRectangle(table, left: 0f, top: 0f, width: boxWidth, height: boxHeight);
            BlackjackPanel.Append(table);

            // Close button
            Asset<Texture2D> buttonCloseTexture = ModContent.Request<Texture2D>("Blackjack/Assets/ButtonClose");
            closeButton = new UIHoverImageButton(buttonCloseTexture, Language.GetTextValue("LegacyInterface.52")); // Localized text for "Close"
            SetRectangle(closeButton, left: boxWidth - 70f, top: 30f, width: 44f, height: 44f);
            closeButton.OnLeftClick += new MouseEvent(CloseButtonClicked);

            // Inactive close button
            Asset<Texture2D> buttonCloseInactiveTexture = ModContent.Request<Texture2D>("Blackjack/Assets/ButtonCloseInactive");
            closeButtonInactive = new UIHoverImageButton(buttonCloseInactiveTexture, Language.GetTextValue("Mods.Blackjack.UI.DisabledClose"));
            SetRectangle(closeButtonInactive, left: boxWidth - 70f, top: 30f, width: 44f, height: 44f);

            // Blackjack game handler
            blackjackGame = new BlackjackGame(uiScale);
            SetRectangle(blackjackGame, 0f, 50f, boxWidth, boxHeight - 50f);
            BlackjackPanel.Append(blackjackGame);

            // Betting item slot
            float betItemSlotSize = 88f;
            betItemSlot = new BetItemSlot(blackjackGame.BetItem, ItemSlot.Context.BankItem, betItemSlotSize);
            SetRectangle(betItemSlot, left: 20f, top: boxHeight - 108f, width: betItemSlotSize, height: betItemSlotSize);
            blackjackGame.SetBetItemSlot(betItemSlot);
            BlackjackPanel.Append(betItemSlot);

            // Play button
            Asset<Texture2D> buttonPlayTexture = ModContent.Request<Texture2D>("Blackjack/Assets/ButtonPlay");
            playButton = new UIHoverImageButton(buttonPlayTexture, Language.GetTextValue("Mods.Blackjack.UI.Play"));
            SetRectangle(playButton, left: boxWidth - 108f, top: boxHeight - 108f, width: 88f, height: 88f);
            playButton.OnLeftClick += new MouseEvent(PlayButtonClicked);
            // BlackjackPanel.Append(playButton); // By default, the play button is hidden until the player places a bet

            // Hit button
            Asset<Texture2D> buttonHitTexture = ModContent.Request<Texture2D>("Blackjack/Assets/ButtonHit");
            hitButton = new UIHoverImageButton(buttonHitTexture, Language.GetTextValue("Mods.Blackjack.UI.Hit"));
            SetRectangle(hitButton, left: boxWidth / 2 - 96f, top: boxHeight - 108f, width: 88f, height: 88f);
            hitButton.OnLeftClick += (evt, element) =>
            {
                if (blackjackGame.IsAnimating)
                    return;
                SoundEngine.PlaySound(SoundID.Item37); // Reforge sound
                blackjackGame.DealCard();
            };

            // Stand button
            Asset<Texture2D> buttonStandTexture = ModContent.Request<Texture2D>("Blackjack/Assets/ButtonStand");
            standButton = new UIHoverImageButton(buttonStandTexture, Language.GetTextValue("Mods.Blackjack.UI.Stand"));
            SetRectangle(standButton, left: boxWidth / 2 + 8f, top: boxHeight - 108f, width: 88f, height: 88f);
            standButton.OnLeftClick += (evt, element) =>
            {
                if (blackjackGame.IsAnimating)
                    return;
                playerStood = true;
                SoundEngine.PlaySound(SoundID.Item144); // Cymbal sound
                // Upon standing, execute dealer logic
                blackjackGame.DealerLogic();
            };

            // Append the close button last so it appears above other elements
            BlackjackPanel.Append(closeButton);



            Append(BlackjackPanel);
        }

        /// <summary>
        /// Helper method to set the position and size of a UIElement.
        /// Using absolute pixel values keeps layout predictable.
        /// </summary>
        /// <param name="uiElement">The element being positioned</param>
        /// <param name="left">Pixel offset from the left of the screen</param>
        /// <param name="top">Pixel offset from the top of the screen</param>
        /// <param name="width">Width in pixels</param>
        /// <param name="height">Height in pixels</param>
        private static void SetRectangle(UIElement uiElement, float left, float top, float width, float height)
        {
            uiElement.Left.Set(left, 0f);
            uiElement.Top.Set(top, 0f);
            uiElement.Width.Set(width, 0f);
            uiElement.Height.Set(height, 0f);
        }

        /// <summary>
        /// Activates the hit/stand buttons
        /// </summary>
        private void ActivateButtons()
        {
            if (buttonsActive)
                return;
            BlackjackPanel.Append(hitButton);
            BlackjackPanel.Append(standButton);
            buttonsActive = true;
        }

        /// <summary>
        /// Deactivates the hit/stand buttons
        /// </summary>
        private void DeactivateButtons()
        {
            if (!buttonsActive)
                return;
            BlackjackPanel.RemoveChild(hitButton);
            BlackjackPanel.RemoveChild(standButton);
            buttonsActive = false;
        }

        private void ActivatePlayButton()
        {
            if (playButtonActive)
                return;
            BlackjackPanel.Append(playButton);
            playButtonActive = true;
        }
        private void DeactivatePlayButton()
        {
            if (!playButtonActive)
                return;
            BlackjackPanel.RemoveChild(playButton);
            playButtonActive = false;
        }

        private void ActivateCloseButton()
        {
            if (closeButtonActive)
                return;
            BlackjackPanel.Append(closeButton);
            try
            {
                BlackjackPanel.RemoveChild(closeButtonInactive);
            }
            catch
            {
                // Do nothing
            }

            closeButtonActive = true;
        }

        private void DeactivateCloseButton()
        {
            if (!closeButtonActive)
                return;
            BlackjackPanel.Append(closeButtonInactive);
            try
            {
                BlackjackPanel.RemoveChild(closeButton);
            }
            catch
            {
                // Do nothing
            }
            closeButtonActive = false;
        }

        /// <summary>
        /// Actions to take when the close button is clicked
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="listeningElement"></param>
        private void CloseButtonClicked(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            blackjackGame.WithdrawBetItem(Main.LocalPlayer);
            ModContent.GetInstance<BlackjackOverlayUISystem>().HideUI();
        }

        /// <summary>
        /// Actions to take when the play button is clicked
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="listeningElement"></param>
        private void PlayButtonClicked(UIMouseEvent evt, UIElement listeningElement)
        {
            if (blackjackGame.IsAnimating)
                return;
            playerStood = false;

            // Ensure the panel stops dragging if the button is removed while
            // the mouse button is still held down.
            BlackjackPanel.CancelDragging();

            SoundEngine.PlaySound(SoundID.ResearchComplete);

            // Start the game. First, shuffle the cards
            blackjackGame.ShuffleCards();

            // Then, deal initial cards
            blackjackGame.InitialDeal();

            // Finally, deactivate the play button
            DeactivatePlayButton();
        }

        /// <summary>
        /// Override that allows the UI window to visually update properly when dragged around. Also updates the hit/stand button states
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (blackjackGame.GetActiveGame() && !blackjackGame.IsAnimating && !playerStood)
                ActivateButtons();
            else
                DeactivateButtons();

            // Main.NewText(betItemSlot.item.stack + " " + !betItemSlot.item.IsAir);
            if (!blackjackGame.GetActiveGame() && !betItemSlot.item.IsAir && betItemSlot.item.stack > 0)
                ActivatePlayButton();
            else
                DeactivatePlayButton();

            if (blackjackGame.GetActiveGame())
                DeactivateCloseButton();
            else
                ActivateCloseButton();
        }

        public override void OnDeactivate()
        {
            blackjackGame.ResetGame();
            base.OnDeactivate();
        }

        
    }
}
