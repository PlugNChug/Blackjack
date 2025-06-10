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
    // Main UIState that houses the draggable panel and game logic.
    // This state is only loaded on the client and manages all
    // user interface elements for the blackjack mini-game.
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
            SetRectangle(hitButton, left: boxWidth / 2 - 96f, top: boxHeight - 96f, width: 88f, height: 88f);
            hitButton.OnLeftClick += (evt, element) =>
            {
                if (blackjackGame.IsAnimating)
                    return;
                SoundEngine.PlaySound(SoundID.Item37); // Reforge sound
                blackjackGame.DealCard();
            };

            // Stand button
            Asset<Texture2D> buttonStandTexture = ModContent.Request<Texture2D>("Blackjack/Assets/ButtonStand");
            standButton = new UIHoverImageButton(buttonStandTexture, "Stand");
            SetRectangle(standButton, left: boxWidth / 2 + 8f, top: boxHeight - 96f, width: 88f, height: 88f);
            standButton.OnLeftClick += (evt, element) =>
            {
                if (blackjackGame.IsAnimating)
                    return;
                SoundEngine.PlaySound(SoundID.Item144); // Cymbal sound
                // Upon standing, execute dealer logic
                blackjackGame.DealerLogic();
            };



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
        private void SetRectangle(UIElement uiElement, float left, float top, float width, float height)
        {
            uiElement.Left.Set(left, 0f);
            uiElement.Top.Set(top, 0f);
            uiElement.Width.Set(width, 0f);
            uiElement.Height.Set(height, 0f);
        }

        private bool buttonsActive = false;

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

        /// <summary>
        /// Actions to take when the close button is clicked
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="listeningElement"></param>
        private void CloseButtonClicked(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
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

            SoundEngine.PlaySound(SoundID.ResearchComplete);

            // Start the game. First, shuffle the cards
            blackjackGame.ShuffleCards();

            // Then, deal initial cards
            blackjackGame.InitialDeal();
        }

        /// <summary>
        /// Override that allows the UI window to visually update properly when dragged around. Also updates the hit/stand button states
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (blackjackGame.GetActiveGame() && !blackjackGame.IsAnimating)
            {
                ActivateButtons();
            }
            else
            {
                DeactivateButtons();
            }
        }

        /// <summary>
        /// Card dealing animation helper
        /// </summary>
        public class DealingCard
        {
            // Index in the deck that identifies which card image to draw
            public int CardIndex;
            // Starting position of the animation
            public Vector2 StartPos;
            // Final destination when the card reaches the table
            public Vector2 EndPos;
            // Progress of the animation from 0 (start) to 1 (finished)
            public float Progress;
            // Determines if the card is dealt to the player or the dealer
            public bool ToPlayer;
        }

        /// <summary>
        /// UIElement representing a single blackjack game session.
        /// Handles card shuffling, dealing, drawing and determining winners.
        /// </summary>
        public class BlackjackGame : UIElement
        {
            // Amount of money the player has for betting
            private int playerMoney;

            // Holds a numeric representation of a standard 52 card deck
            private List<int> cardList;
            // Tracks which card to draw next from the shuffled deck
            private int cardIndex = 0;

            // Player/dealer card fields
            // Lists storing each card currently held by the player and dealer
            private List<int> playerCards = new List<int>();
            private List<int> dealerCards = new List<int>();
            private bool dealerFirstCardRevealed = false; // Has the dealer's first card been shown
            private bool flippingDealerCard = false;       // Is the reveal animation running
            private float dealerCardFlipProgress = 0f;      // Animation progress value
            private const float dealerCardFlipSpeed = 0.15f;

            // Hand values for calculations
            private int playerHandValue = 0;
            private int dealerHandValue = 0;

            // Active game boolean
            private bool isGameActive = false;

            // Animation fields
            // Cards waiting to be animated onto the table
            private Queue<DealingCard> dealingQueue = new Queue<DealingCard>();
            // Card currently in transit
            private DealingCard currentDealingCard = null;
            // Speed at which cards slide to their destination
            private float dealSpeed = 0.08f; // Adjust for faster/slower animation

            private bool dealerTurn = false;       // True when the dealer is drawing
            private int dealerDrawDelayTimer = 0;  // Countdown before the next dealer draw
            private const int DealerDrawDelay = 30; // Frames of delay between dealer draws

            // Game status text
            DynamicSpriteFont fontBig = FontAssets.DeathText.Value;
            string gameStatus = "";


            // Returns true if a game is currently in progress
            public bool GetActiveGame() => isGameActive;

            // True while a card animation or dealer flip animation is occurring
            public bool IsAnimating => currentDealingCard != null || dealingQueue.Count > 0 || flippingDealerCard;

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
                // Randomly shuffle the deck before a new game starts
                Random rng = new Random();
                int n = cardList.Count;
                // Classic Fisher–Yates shuffle algorithm
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
                // Called when the player presses Play.
                // Sets up the hands and deals the opening two cards.
                // Set game as active
                isGameActive = true;

                // Clear game status text
                gameStatus = "";

                // Reset dealing queue
                dealingQueue.Clear();
                currentDealingCard = null;
                dealerTurn = false;
                flippingDealerCard = false;
                dealerCardFlipProgress = 0f;

                // Deal one card to player and one to dealer and repeat
                playerCards.Clear();
                dealerCards.Clear();
                for (int i = 0; i < 2; i++)
                {
                    if (cardIndex < cardList.Count)
                    {
                        int playerCard = cardList[cardIndex++];
                        Vector2 playerEndPos = new Vector2(10 + i * 150, 400);
                        QueueDealCard(playerCard, true, playerEndPos);

                        int dealerCard = cardList[cardIndex++];
                        Vector2 dealerEndPos = new Vector2(10 + i * 150, 80);
                        QueueDealCard(dealerCard, false, dealerEndPos);
                    }
                }
                dealerFirstCardRevealed = false;
            }

            public void DealCard()
            {
                // Deal one card
                if (cardIndex < cardList.Count)
                {
                    int card = cardList[cardIndex++];
                    Vector2 endPos = new Vector2(10 + playerCards.Count * 150, 400); // Match your draw positions
                    QueueDealCard(card, true, endPos);
                }

                // Further logic handled when the card finishes animating
            }

            public void DealerLogic()
            {
                // Called when the player stands. Begins the dealer's drawing phase.
                dealerTurn = true;
                dealerDrawDelayTimer = DealerDrawDelay;
                StartDealerFlip();
                TryQueueDealerCard();
            }

            private void TryQueueDealerCard()
            {
                // Handles the dealer drawing additional cards while respecting a small delay between draws
                if (dealerDrawDelayTimer > 0)
                {
                    dealerDrawDelayTimer--;
                    return;
                }

                if (dealerHandValue < 17 || (dealerHandValue == 17 && dealerCards.Exists(card => card % 13 == 0)))
                {
                    if (cardIndex < cardList.Count)
                    {
                        int card = cardList[cardIndex++];
                        Vector2 endPos = new Vector2(10 + dealerCards.Count * 150, 80);
                        QueueDealCard(card, false, endPos);
                        dealerDrawDelayTimer = DealerDrawDelay;
                    }
                }
                else
                {
                    DetermineWinner();
                    dealerTurn = false;
                }
            }

            private void DetermineWinner()
            {
                // Compare hand values and announce the result
                StartDealerFlip();
                if (dealerHandValue > 21 || playerHandValue > dealerHandValue)
                {
                    gameStatus = "You win!";
                }
                else if (playerHandValue < dealerHandValue)
                {
                    gameStatus = "You lose...";
                }
                else
                {
                    gameStatus = "Push! It's a tie.";
                }
                isGameActive = false;
            }

            private void StartDealerFlip()
            {
                // Begins the animation that reveals the dealer's hidden card
                if (flippingDealerCard || dealerFirstCardRevealed)
                    return;
                flippingDealerCard = true;
                dealerCardFlipProgress = 0f;
            }

            private void CalculateHandValues()
            {
                // Update cached totals for both hands
                playerHandValue = CalculateValue(playerCards);
                dealerHandValue = CalculateValue(dealerCards);

                if (!isGameActive)
                    return;

                // Check for automatic push due to both the player and dealer having 21 with two cards
                if (playerCards.Count == 2 && dealerCards.Count == 2 && playerHandValue == 21 && dealerHandValue == 21)
                {
                    gameStatus = "Push. Player and Dealer tied Blackjack.";
                    isGameActive = false;
                    StartDealerFlip();
                }
                // Then, check for player natural
                else if (playerCards.Count == 2 && playerHandValue == 21)
                {
                    gameStatus = "Blackjack! You win!";
                    SoundEngine.PlaySound(SoundID.Meowmere);
                    playerMoney += (int)(playerMoney * 1.5);
                    isGameActive = false;
                    StartDealerFlip();
                }
                else if (dealerCards.Count == 2 && dealerHandValue == 21)
                {
                    gameStatus = "Dealer Blackjack. You lose...";
                    playerMoney -= playerMoney;
                    isGameActive = false;
                    StartDealerFlip();
                }
            }

            private int CalculateValue(List<int> hand)
            {
                // Computes the best numeric value for a list of cards
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

            private void QueueDealCard(int cardIndex, bool toPlayer, Vector2 endPos)
            {
                CalculatedStyle dims = GetDimensions();
                Vector2 start = new Vector2(dims.X + 800, dims.Y + 250);
                Vector2 finalPos = new Vector2(dims.X + endPos.X, dims.Y + endPos.Y);

                dealingQueue.Enqueue(new DealingCard
                {
                    CardIndex = cardIndex,
                    StartPos = start,
                    EndPos = finalPos,
                    Progress = 0f,
                    ToPlayer = toPlayer
                });
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);

                DynamicSpriteFont font = FontAssets.ItemStack.Value;

                CalculatedStyle dimensions = GetDimensions();
                Vector2 position = new Vector2(dimensions.X, dimensions.Y);

                // Bet amount info
                string playerText = "Current bet: " + playerMoney;
                spriteBatch.DrawString(font, playerText, position + new Vector2(10, 600), Color.White);

                // Dealer hand value text. Should be rendered above the dealer's cards
                string dealerStatus1 = "Dealer's hand: ";
                string dealerStatus2;
                if (isGameActive && !dealerFirstCardRevealed && dealerCards.Count > 1)
                {
                    dealerStatus2 = (dealerCards[1] % 13 >= 10 ? 10 : (dealerCards[1] % 13) + 1).ToString() + " + ???";
                }
                else if (dealerCards.Count <= 1)
                {
                    dealerStatus2 = "...";
                }
                else
                {
                    dealerStatus2 = dealerHandValue.ToString();
                }

                // Player hand value text. Should be rendered above the player's cards
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
                    Rectangle cardRectangle = new Rectangle((int)position.X + 10 + i * 150, (int)position.Y + 400, 90, 128);

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
                    Rectangle cardRectangle;

                    if (i == 0 && (!dealerFirstCardRevealed || flippingDealerCard))
                    {
                        float scale;
                        if (flippingDealerCard)
                        {
                            if (dealerCardFlipProgress < 0.5f)
                            {
                                cardTexture = cardBackAsset.Value;
                                scale = 1f - dealerCardFlipProgress * 2f;
                            }
                            else
                            {
                                cardTexture = cardFrontAsset.Value;
                                scale = (dealerCardFlipProgress - 0.5f) * 2f;
                            }
                        }
                        else
                        {
                            cardTexture = cardBackAsset.Value;
                            scale = 1f;
                        }

                        int width = (int)(90 * MathHelper.Clamp(scale, 0f, 1f));
                        int x = (int)position.X + 10 + i * 150 + (90 - width) / 2;
                        cardRectangle = new Rectangle(x, (int)position.Y + 80, width, 128);
                    }
                    else
                    {
                        cardTexture = cardFrontAsset.Value;
                        cardRectangle = new Rectangle((int)position.X + 10 + i * 150, (int)position.Y + 80, 90, 128);
                    }

                    spriteBatch.Draw(cardTexture, cardRectangle, Color.White);
                }


                // Draw animated card
                if (currentDealingCard != null)
                {
                    Vector2 pos = Vector2.Lerp(
                        currentDealingCard.StartPos,
                        currentDealingCard.EndPos,
                        currentDealingCard.Progress
                    );
                    Asset<Texture2D> cardFrontAsset = ModContent.Request<Texture2D>($"Blackjack/Assets/Cards/card_back");
                    Rectangle cardRectangle = new Rectangle((int)pos.X, (int)pos.Y, 90, 128);
                    spriteBatch.Draw(cardFrontAsset.Value, cardRectangle, Color.White);
                }

                // Draw the card stack
                Asset<Texture2D> cardStackAsset = ModContent.Request<Texture2D>("Blackjack/Assets/Cards/card_stack");
                Rectangle stackRectangle = new Rectangle((int)position.X + 800, (int)position.Y + 250, 90, 135);
                spriteBatch.Draw(cardStackAsset.Value, stackRectangle, Color.White);

                // Render game status text
                spriteBatch.DrawString(fontBig, gameStatus, new Vector2(position.X + dimensions.Width / 2, position.Y + dimensions.Height / 2), Color.Blue);

            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);

                if (flippingDealerCard)
                {
                    dealerCardFlipProgress += dealerCardFlipSpeed;
                    if (dealerCardFlipProgress >= 1f)
                    {
                        dealerCardFlipProgress = 1f;
                        flippingDealerCard = false;
                        dealerFirstCardRevealed = true;
                    }
                }

                if (currentDealingCard == null && dealingQueue.Count > 0)
                {
                    currentDealingCard = dealingQueue.Dequeue();
                }

                if (currentDealingCard != null)
                {
                    currentDealingCard.Progress += dealSpeed;
                    if (currentDealingCard.Progress >= 1f)
                    {
                        // Animation done, add to hand
                        if (currentDealingCard.ToPlayer)
                            playerCards.Add(currentDealingCard.CardIndex);
                        else
                            dealerCards.Add(currentDealingCard.CardIndex);

                        CalculateHandValues();
                        DealingCard finished = currentDealingCard;
                        currentDealingCard = null;

                        if (finished.ToPlayer)
                        {
                            if (playerHandValue > 21)
                            {
                                gameStatus = "Bust. You lose...";
                                isGameActive = false;
                                StartDealerFlip();
                            }
                            else if (playerHandValue == 21)
                            {
                                if (isGameActive)
                                    DealerLogic();
                            }
                        }
                        else if (dealerTurn)
                        {
                            TryQueueDealerCard();
                        }
                    }
                }

                if (dealerTurn && currentDealingCard == null && dealingQueue.Count == 0)
                {
                    TryQueueDealerCard();
                }
            }

        }
    }
}
