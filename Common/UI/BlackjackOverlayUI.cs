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
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace Blackjack.Common.UI
{
    internal class BlackjackOverlayUIState : UIState
    {
        public DraggableUIPanel BlackjackPanel;
        private BlackjackGame blackjackGame;

        private UIHoverImageButton playButton;
        private UIHoverImageButton hitButton;
        private UIHoverImageButton standButton;

        public override void OnInitialize()
        {
            BlackjackPanel = new DraggableUIPanel();
            BlackjackPanel.SetPadding(0);

            float boxWidth = 1280f;
            float boxHeight = 720f;

            SetRectangle(BlackjackPanel, left: 400f, top: 100f, width: boxWidth, height: boxHeight);
            BlackjackPanel.BackgroundColor = new Color(16, 119, 40);
            // Close button
            Asset<Texture2D> buttonCloseTexture = ModContent.Request<Texture2D>("Terraria/Images/UI/SearchCancel");
            UIHoverImageButton closeButton = new UIHoverImageButton(buttonCloseTexture, Language.GetTextValue("LegacyInterface.52")); // Localized text for "Close"
            SetRectangle(closeButton, left: boxWidth - 40f, top: 10f, width: 22f, height: 22f);
            closeButton.OnLeftClick += new MouseEvent(CloseButtonClicked);
            BlackjackPanel.Append(closeButton);

            blackjackGame = new BlackjackGame();
            SetRectangle(blackjackGame, 20f, 50f, 200f, 30f);
            BlackjackPanel.Append(blackjackGame);

            // Play button
            Asset<Texture2D> buttonPlayTexture = ModContent.Request<Texture2D>("Blackjack/Assets/ButtonPlay");
            playButton = new UIHoverImageButton(buttonPlayTexture, "Play");
            SetRectangle(playButton, left: boxWidth - 96f, top: boxHeight - 96f, width: 88f, height: 88f);
            playButton.OnLeftClick += new MouseEvent(PlayButtonClicked);
            BlackjackPanel.Append(playButton);

            // Hit button
            Asset<Texture2D> buttonHitTexture = ModContent.Request<Texture2D>("Blackjack/Assets/ButtonHit");
            hitButton = new UIHoverImageButton(buttonHitTexture, "Hit");
            SetRectangle(hitButton, left: boxWidth / 2 - 44f, top: boxHeight - 96f, width: 88f, height: 88f);
            hitButton.OnLeftClick += (evt, element) =>
            {
                SoundEngine.PlaySound(SoundID.Item37); // Reforge sound
                blackjackGame.DealCard();
            };

            // Stand button
            Asset<Texture2D> buttonStandTexture = ModContent.Request<Texture2D>("Blackjack/Assets/ButtonStand");
            standButton = new UIHoverImageButton(buttonStandTexture, "Stand");
            SetRectangle(standButton, left: boxWidth / 2 + 62f, top: boxHeight - 96f, width: 88f, height: 88f);
            standButton.OnLeftClick += (evt, element) =>
            {
                SoundEngine.PlaySound(SoundID.Item144); // Cymbal sound
                // Upon standing, execute dealer logic
                blackjackGame.DealerLogic();
            };



            Append(BlackjackPanel);
        }

        private void SetRectangle(UIElement uiElement, float left, float top, float width, float height)
        {
            uiElement.Left.Set(left, 0f);
            uiElement.Top.Set(top, 0f);
            uiElement.Width.Set(width, 0f);
            uiElement.Height.Set(height, 0f);
        }

        private void ActivateButtons()
        {
            BlackjackPanel.Append(hitButton);
            BlackjackPanel.Append(standButton);
        }

        private void DeactivateButtons()
        {
            BlackjackPanel.RemoveChild(hitButton);
            BlackjackPanel.RemoveChild(standButton);
        }

        private void CloseButtonClicked(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            ModContent.GetInstance<BlackjackOverlayUISystem>().HideUI();
        }
        private void PlayButtonClicked(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.ResearchComplete);
            
            // Start the game. First, shuffle the cards
            blackjackGame.ShuffleCards();

            // Then, deal initial cards
            blackjackGame.InitialDeal();

            // Enable Hit and Stand buttons
            ActivateButtons();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!blackjackGame.GetActiveGame())
            {
                DeactivateButtons();
            }
        }

        public class BlackjackGame : UIElement
        {
            private int playerMoney;
            private List<int> cardList;
            private int cardIndex = 0;

            private List<int> playerCards = new List<int>();
            private List<int> dealerCards = new List<int>();
            private bool dealerFirstCardRevealed = false;

            private int playerHandValue = 0;
            private int dealerHandValue = 0;

            private bool isGameActive = false;

            public bool GetActiveGame()
            {
                return isGameActive;
            }

            public override void OnInitialize()
            {
                base.OnInitialize();
                playerMoney = 1000; // Starting money

                // Initialize a standard deck of 52 cards
                cardList = new List<int>();
                for (int i = 0; i < 52; i++)
                {
                    cardList.Add(i);
                }
            }

            public void ShuffleCards()
            {
                Random rng = new Random();
                int n = cardList.Count;
                // Fisher-Yates shuffle algorithm
                while (n > 1)
                {
                    int k = rng.Next(n--);
                    int temp = cardList[n];
                    cardList[n] = cardList[k];
                    cardList[k] = temp;
                }

                // Reset card index
                cardIndex = 0;
            }

            public void InitialDeal()
            {
                // Set game as active
                isGameActive = true;

                // Deal one card to player and one to dealer and repeat
                playerCards.Clear();
                dealerCards.Clear();
                for (int i = 0; i < 2; i++)
                {
                    if (cardIndex < cardList.Count)
                        playerCards.Add(cardList[cardIndex++]);
                    if (cardIndex < cardList.Count)
                        dealerCards.Add(cardList[cardIndex++]);
                }
                dealerFirstCardRevealed = false;

                // Calculate hand values
                CalculateHandValues();
            }

            public void DealCard()
            {
                // Deal one card to the player
                if (cardIndex >= cardList.Count)
                {
                    Main.NewText("No more cards in the deck!");
                }
                else 
                {
                    playerCards.Add(cardList[cardIndex++]);
                }

                // Calculate hand values
                CalculateHandValues();

                // Check for bust or 21
                if (playerHandValue > 21)
                {
                    Main.NewText("You busted! Dealer wins.");
                    isGameActive = false;
                    dealerFirstCardRevealed = true;
                }
                else if (playerHandValue == 21)
                {
                    DealerLogic();
                }
            }

            public void DealerLogic()
            {
                // Reveal first card
                dealerFirstCardRevealed = true;

                // Hit on soft 17
                while (dealerHandValue < 17 || (dealerHandValue == 17 && dealerCards.Exists(card => card % 13 == 0)))
                {
                    if (cardIndex < cardList.Count)
                        dealerCards.Add(cardList[cardIndex++]);
                    CalculateHandValues();
                }

                // Determine winner
                if (dealerHandValue > 21 || playerHandValue > dealerHandValue)
                {
                    Main.NewText("You win!");
                }
                else if (playerHandValue < dealerHandValue)
                {
                    Main.NewText("Dealer wins.");
                }
                else
                {
                    Main.NewText("Push! It's a tie.");
                }
                isGameActive = false;
            }

            private void CalculateHandValues()
            {
                playerHandValue = CalculateValue(playerCards);
                dealerHandValue = CalculateValue(dealerCards);

                // Check for automatic push due to both the player and dealer having 21 with two cards
                if (playerCards.Count == 2 && dealerCards.Count == 2 && playerHandValue == 21 && dealerHandValue == 21)
                {
                    Main.NewText("Push! Both you and the dealer have Blackjack.");
                    isGameActive = false;
                    dealerFirstCardRevealed = true;
                }
                // Then, check for player natural
                else if (playerCards.Count == 2 && playerHandValue == 21)
                {
                    Main.NewText("Blackjack!");
                    playerMoney += (int)(playerMoney * 1.5);
                    isGameActive = false;
                    dealerFirstCardRevealed = true;
                }
                else if (dealerCards.Count == 2 && dealerHandValue == 21)
                {
                    Main.NewText("Dealer has Blackjack! You lose.");
                    playerMoney -= playerMoney;
                    isGameActive = false;
                    dealerFirstCardRevealed = true;
                }
            }

            private int CalculateValue(List<int> hand)
            {
                int value = 0;
                int aceCount = 0;

                foreach (int card in hand)
                {
                    int rank = card % 13;
                    if (rank >= 10) // Face cards
                    {
                        value += 10;
                    }
                    else if (rank == 0) // Ace
                    {
                        aceCount++;
                        value += 11; // Initially count ace as 11
                    }
                    else
                    {
                        value += rank + 1; // Number cards
                    }
                }

                // Adjust for aces if value is over 21
                while (value > 21 && aceCount > 0)
                {
                    value -= 10;
                    aceCount--;
                }

                return value;
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);

                DynamicSpriteFont font = FontAssets.ItemStack.Value;

                CalculatedStyle dimensions = GetDimensions();
                Vector2 position = new Vector2(dimensions.X, dimensions.Y);

                // Render player info
                string playerText = "Current bet: " + playerMoney;
                spriteBatch.DrawString(font, playerText, position + new Vector2(10, 600), Color.White);

                // Dealer status messages
                string dealerStatus1 = "Dealer's hand: ";
                string dealerStatus2;
                if (isGameActive && !dealerFirstCardRevealed)
                {
                    dealerStatus2 = (dealerCards[1] % 13 >= 10 ? 10 : (dealerCards[1] % 13) + 1).ToString() + " + ???";
                }
                else
                {
                    dealerStatus2 = dealerHandValue.ToString();
                }

                // Player status messages
                string playerStatus1 = "Your hand: ";
                string playerStatus2 = playerHandValue.ToString();

                spriteBatch.DrawString(font, dealerStatus1, position + new Vector2(10, 30), Color.White);
                spriteBatch.DrawString(font, dealerStatus2, position + new Vector2(10, 45), Color.Yellow);
                spriteBatch.DrawString(font, playerStatus1, position + new Vector2(10, 350), Color.White);
                spriteBatch.DrawString(font, playerStatus2, position + new Vector2(10, 365), Color.Yellow);

                // Render player cards
                for (int i = 0; i < playerCards.Count; i++)
                {
                    // Get card texture
                    int cardIndex = playerCards[i];
                    Asset<Texture2D> cardTextureAsset = ModContent.Request<Texture2D>($"Blackjack/Assets/Cards/card_{cardIndex}");

                    // Create small rectangle to draw the card in
                    Rectangle cardRectangle = new Rectangle((int)position.X + 10 + i * 150, (int)position.Y + 400, 90, 135);

                    spriteBatch.Draw(cardTextureAsset.Value, cardRectangle, Color.White);
                    
                }

                // Render dealer cards
                for (int i = 0; i < dealerCards.Count; i++)
                {
                    // Get card texture
                    int cardIndex = dealerCards[i];
                    Asset<Texture2D> cardBackAsset = ModContent.Request<Texture2D>($"Blackjack/Assets/Cards/card_back");
                    Asset<Texture2D> cardFrontAsset = ModContent.Request<Texture2D>($"Blackjack/Assets/Cards/card_{cardIndex}");
                    Texture2D cardTexture;

                    // Hide first card logic
                    if (i == 0 && !dealerFirstCardRevealed)
                    {
                        cardTexture = cardBackAsset.Value;
                    }
                    else
                    {
                        cardTexture = cardFrontAsset.Value;
                    }

                    // Create small rectangle to draw the card in
                    Rectangle cardRectangle = new Rectangle((int)position.X + 10 + i * 150, (int)position.Y + 80, 90, 135);

                    spriteBatch.Draw(cardTexture, cardRectangle, Color.White);
                }
            }
        }
    }
}
