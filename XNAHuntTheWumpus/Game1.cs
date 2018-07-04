using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace XNAHuntTheWumpus
{

    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont courierFont;
        Texture2D roomDoor;
        Texture2D pit;
        Texture2D wump;
        Texture2D arrow;
        Texture2D zubat;
        Vector2 textPos;
        Vector2 endGameText;
        State currentGameState;
        int batRoom;
        int wumpRoom;
        int[] arrowRooms = new int[5];
        int[] arrowPath = new int[6];
        int selectedRooms = 0;
        double timer;

        Map gameBoard;
        //Room locations
        Vector2[] roomVectors;
        Texture2D lineTexture;
        Vector2 arrowVector;

        public enum State
        {
            Playing,
            Fell,
            Superbat,
            Shooting,
            ArrowSelect,
            WumpBump,
            WumpWin,
            PlayerWin,
            PlayerHit
        }
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = 800;
            graphics.PreferredBackBufferWidth = 800;
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            this.IsMouseVisible = true;
            gameBoard = new Map();
            currentGameState = State.Playing;
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            Mouse.WindowHandle = Window.Handle;
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            courierFont = Content.Load<SpriteFont>("GameFont");
            roomDoor = Content.Load<Texture2D>("room");
            pit = Content.Load<Texture2D>("pit");
            arrow = Content.Load<Texture2D>("arrow");
            wump = Content.Load<Texture2D>("wump");
            zubat = Content.Load<Texture2D>("zubat");
            lineTexture = new Texture2D(GraphicsDevice, 1, 1);
            lineTexture.SetData<Color>(new Color[] { Color.White });

            roomVectors = new Vector2[21]
            {
                new Vector2(), //dummy to counter zero-indexing
                new Vector2(graphics.GraphicsDevice.Viewport.Width/2 - roomDoor.Width/2, 0), 
                new Vector2(graphics.GraphicsDevice.Viewport.Width-roomDoor.Width, graphics.GraphicsDevice.Viewport.Height/3 - roomDoor.Width/2),
                new Vector2(3 *(graphics.GraphicsDevice.Viewport.Width / 4), graphics.GraphicsDevice.Viewport.Height-roomDoor.Height),
                new Vector2(graphics.GraphicsDevice.Viewport.Width / 4 - roomDoor.Width, graphics.GraphicsDevice.Viewport.Height - roomDoor.Height),
                new Vector2(0, graphics.GraphicsDevice.Viewport.Height/3 - roomDoor.Width / 2), //5
                new Vector2(130, 350),
                new Vector2(190, 230),
                new Vector2(graphics.GraphicsDevice.Viewport.Width/2 - roomDoor.Width/2, 150),
                new Vector2(540, 230),
                new Vector2(610, 350), //10
                new Vector2(610, 475),
                new Vector2(540, 585),
                new Vector2(360, 650),
                new Vector2(190, 585),
                new Vector2(130, 475), //15
                new Vector2(250, 430),
                new Vector2(300, 300),
                new Vector2(420, 300),
                new Vector2(475, 430),
                new Vector2(360, 525)
            };

            textPos = new Vector2(graphics.GraphicsDevice.Viewport.Width - 190, 0);
            arrowVector = new Vector2(335, 425);
            endGameText = new Vector2(graphics.GraphicsDevice.Viewport.Width - 110, 50);


            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }
        Vector2 MoveVector(Vector2 position, Vector2 target, float speed)
        {
            double direction = Math.Atan2(target.Y - position.Y, target.X - position.X);

            Vector2 move = new Vector2(0, 0);

            move.X = (float)Math.Cos(direction) * speed;
            move.Y = (float)Math.Sin(direction) * speed;

            return move;

        }
        void playerMove(int dest)
        {
            //update player position
            gameBoard.player.pos = dest;
            if (gameBoard.Pits.Contains(dest))
                currentGameState = State.Fell; //update state
            else if (gameBoard.Bats.Contains(dest))
            {
                currentGameState = Game1.State.Playing; //update state
                batRoom = dest;
                //generate new room for player to be carried to
                int newRoom = -1;
                Random randomGenerator = new Random();
                do
                {
                    newRoom = randomGenerator.Next(1, 20);
                } while (newRoom == dest);
                playerMove(newRoom);
            }
            else if (gameBoard.wump.pos == dest)
            {
                //check if already awake
                if (gameBoard.wump.awake)
                    currentGameState = Game1.State.WumpWin;
                else //wump was sleeping
                {
                    gameBoard.wump.awake = true;
                    gameBoard.WumpusMove();
                    currentGameState = State.Playing;
                    //if wumpus didn't move then he wins
                    if (gameBoard.wump.pos == gameBoard.player.pos)
                        currentGameState = State.WumpWin;
                }
            }
            else //regular move
                currentGameState = State.Playing;
            
        }
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here
            if (gameBoard.wump.awake && gameBoard.player.pos == gameBoard.wump.pos || gameBoard.player.arrows == 0)
                currentGameState = State.WumpWin;
            if (currentGameState == State.Playing)
            {
                if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                {
                    Vector2 click = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
                    //check if clicked on arrow
                    if (click.X >= arrowVector.X && click.X <= arrowVector.X + arrow.Width &&
                        click.Y >= arrowVector.Y && click.Y <= arrowVector.Y + arrow.Height)
                        currentGameState = State.ArrowSelect;
                        
                    else
                    {
                        //check if clicked on adjacent room
                        int[] adjacent = gameBoard.getAdjacentRooms(gameBoard.player.pos);
                        for (int i = 0; i < adjacent.Length; i++)
                            if (click.X >= roomVectors[adjacent[i]].X && click.X <= roomVectors[adjacent[i]].X + roomDoor.Width &&
                                click.Y >= roomVectors[adjacent[i]].Y && click.Y <= roomVectors[adjacent[i]].Y + roomDoor.Height)
                            { //valid click on room

                                gameBoard.WumpusMove();
                                //check if ran into bat
                                if (gameBoard.Bats.Contains(adjacent[i]))
                                {
                                    currentGameState = State.Superbat;
                                    batRoom = adjacent[i];
                                    timer = 0;
                                }
                                //check if bumped into wump
                                else if (gameBoard.wump.pos == adjacent[i])
                                {
                                    currentGameState = State.WumpBump;
                                    wumpRoom = adjacent[i];
                                    timer = 0;
                                }
                                else
                                    playerMove(adjacent[i]);
                                break;
                            }
                    }
                }
            }
            else if (currentGameState == State.ArrowSelect)
            {
                if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                    if (selectedRooms < 5)
                    {
                        Vector2 click2 = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
                        if (click2.X >= arrowVector.X && click2.X <= arrowVector.X + arrow.Width &&
                            click2.Y >= arrowVector.Y && click2.Y <= arrowVector.Y + arrow.Height)
                        {
                            if (selectedRooms > 0)
                                fireArrow(ref arrowRooms);

                        }
                        else
                        {
                            //check if click on room
                            for (int i = 1; i < roomVectors.Length; i++)
                                if (click2.X >= roomVectors[i].X && click2.X <= roomVectors[i].X + roomDoor.Width &&
                                    click2.Y >= roomVectors[i].Y && click2.Y <= roomVectors[i].Y + roomDoor.Height)
                                {
                                    if (selectedRooms > 0)
                                        if (i != arrowRooms[selectedRooms - 1]) //check to make sure different room
                                            arrowRooms[selectedRooms++] = i;
                                        else;
                                    else
                                        arrowRooms[selectedRooms++] = i;
                                }
                        }
                    }
                    else
                        fireArrow(ref arrowRooms);
            }
            else if (currentGameState == State.Shooting)
            {
                if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                {
                    int[] adjacent = gameBoard.getAdjacentRooms(gameBoard.player.pos);
                    Vector2 click = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
                    for (int i = 0; i < adjacent.Length; i++)
                        if (click.X >= roomVectors[adjacent[i]].X && click.X <= roomVectors[adjacent[i]].X + roomDoor.Width &&
                            click.Y >= roomVectors[adjacent[i]].Y && click.Y <= roomVectors[adjacent[i]].Y + roomDoor.Height)
                        {
                            currentGameState = State.Playing;
                            selectedRooms = 0;
                        }
                }
            }
            else if (currentGameState == State.Superbat)
            {
                timer += gameTime.ElapsedGameTime.TotalSeconds;
                if (timer >= 2) //superbat move after drawing sprite for 3 seconds
                    playerMove(batRoom); //update state
            }
            else if (currentGameState == State.WumpBump)
            {
                timer += gameTime.ElapsedGameTime.TotalSeconds;
                if (timer >= 3) //superbat move after drawing sprite for 4 seconds
                    playerMove(wumpRoom); //update state

            }
            else if (currentGameState == State.Fell || currentGameState == State.WumpWin || currentGameState == State.PlayerWin || currentGameState == State.PlayerHit)
            {
                if (Keyboard.GetState().GetPressedKeys().Contains(Keys.D1))
                {
                    gameBoard.ReplayGame();
                    selectedRooms = 0;
                    currentGameState = State.Playing;
                }
                else if (Keyboard.GetState().GetPressedKeys().Contains(Keys.D2))
                {
                    gameBoard.NewGame();
                    currentGameState = State.Playing;
                }
            }

            base.Update(gameTime);
        }

        private void fireArrow(ref int[] rooms)
        {
            currentGameState = State.Shooting;
            if (selectedRooms > 0)
            {
                gameBoard.ShootArrow(ref rooms, selectedRooms, ref currentGameState); //pass arrow rooms, func will pass back arrow path
                arrowPath = rooms; //update arrow path
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();
            DrawMap();
            if (currentGameState == State.Fell) //fell
            {
                spriteBatch.Draw(pit, roomVectors[gameBoard.player.pos], Color.LightGoldenrodYellow);
                spriteBatch.DrawString(courierFont, "Aaaaaahhh!", endGameText, Color.Red);
                spriteBatch.DrawString(courierFont, "\n1 to replay board", endGameText - new Vector2(75, 0), Color.Orange);
                spriteBatch.DrawString(courierFont, "\n\n2 to play new board", endGameText - new Vector2(95, 0), Color.Orange);
            }
            else if (currentGameState == State.ArrowSelect)
                spriteBatch.DrawString(courierFont, "Click up to 5 rooms", endGameText - new Vector2(85, 0), Color.Orange);
            else if (currentGameState == State.Shooting)
                for (int i = 1; i < selectedRooms+1; i++)
                    DrawArrow(spriteBatch, roomCenter(arrowPath[i - 1]), roomCenter(arrowPath[i]));
            else if (currentGameState == State.Superbat)
                spriteBatch.Draw(zubat, roomVectors[batRoom], Color.White);
            else if (currentGameState == State.PlayerWin)
            {
                //draw path
                for (int i = 1; i < selectedRooms + 1; i++)
                    DrawArrow(spriteBatch, roomCenter(arrowPath[i - 1]), roomCenter(arrowPath[i]));
                spriteBatch.Draw(wump, roomVectors[gameBoard.wump.pos], Color.LightGoldenrodYellow);
                spriteBatch.DrawString(courierFont, "You win!", endGameText - new Vector2(15, 0), Color.Red);
                spriteBatch.DrawString(courierFont, "\n1 to replay board", endGameText - new Vector2(65, 0), Color.Orange);
                spriteBatch.DrawString(courierFont, "\n\n2 to play new board", endGameText - new Vector2(85, 0), Color.Orange);

            }
            else if (currentGameState == State.PlayerHit)
            {
                //draw path
                for (int i = 1; i < selectedRooms + 1; i++)
                    DrawArrow(spriteBatch, roomCenter(arrowPath[i - 1]), roomCenter(arrowPath[i]));
                spriteBatch.Draw(wump, roomVectors[gameBoard.wump.pos], Color.LightGoldenrodYellow);
                spriteBatch.DrawString(courierFont, "Suicide!", endGameText - new Vector2(15, 0), Color.Red);
                spriteBatch.DrawString(courierFont, "\n1 to replay board", endGameText - new Vector2(65, 0), Color.Orange);
                spriteBatch.DrawString(courierFont, "\n\n2 to play new board", endGameText - new Vector2(85, 0), Color.Orange);

            }
            else if (currentGameState == State.WumpBump)
                spriteBatch.Draw(wump, roomVectors[wumpRoom], Color.LightGoldenrodYellow);
            if (currentGameState == State.WumpWin)
            {
                //wumpus got you!
                spriteBatch.Draw(wump, roomVectors[gameBoard.wump.pos], Color.LightGoldenrodYellow);
                spriteBatch.DrawString(courierFont, "Wumpus wins!", endGameText - new Vector2(15, 0), Color.Red);
                spriteBatch.DrawString(courierFont, "\n1 to replay board", endGameText - new Vector2(65, 0), Color.Orange);
                spriteBatch.DrawString(courierFont, "\n\n2 to play new board", endGameText - new Vector2(85, 0), Color.Orange);
            }
            else
                spriteBatch.DrawString(courierFont, gameBoard.returnHazards(), new Vector2(0, 0), Color.White);

            spriteBatch.End();
            base.Draw(gameTime);
        }

        void DrawMap()
        {
            //draw layout (rooms, text)
            spriteBatch.DrawString(courierFont, "Hunt The Wumpus 2D", textPos, Color.Goldenrod);
            //connect rooms
            DrawLine(spriteBatch, roomCenter(1), roomCenter(5));
            DrawLine(spriteBatch, roomCenter(1), roomCenter(2));
            DrawLine(spriteBatch, roomCenter(1), roomCenter(8));
            DrawLine(spriteBatch, roomCenter(2), roomCenter(3));
            DrawLine(spriteBatch, roomCenter(2), roomCenter(10));
            DrawLine(spriteBatch, roomCenter(3), roomCenter(4));
            DrawLine(spriteBatch, roomCenter(3), roomCenter(12));
            DrawLine(spriteBatch, roomCenter(4), roomCenter(5));
            DrawLine(spriteBatch, roomCenter(4), roomCenter(14));
            DrawLine(spriteBatch, roomCenter(5), roomCenter(6));
            DrawLine(spriteBatch, roomCenter(6), roomCenter(7));
            DrawLine(spriteBatch, roomCenter(6), roomCenter(15));
            DrawLine(spriteBatch, roomCenter(7), roomCenter(8));
            DrawLine(spriteBatch, roomCenter(7), roomCenter(17));
            DrawLine(spriteBatch, roomCenter(8), roomCenter(9));
            DrawLine(spriteBatch, roomCenter(9), roomCenter(10));
            DrawLine(spriteBatch, roomCenter(9), roomCenter(18));
            DrawLine(spriteBatch, roomCenter(10), roomCenter(11));
            DrawLine(spriteBatch, roomCenter(11), roomCenter(12));
            DrawLine(spriteBatch, roomCenter(11), roomCenter(19));
            DrawLine(spriteBatch, roomCenter(12), roomCenter(13));
            DrawLine(spriteBatch, roomCenter(13), roomCenter(20));
            DrawLine(spriteBatch, roomCenter(13), roomCenter(14));
            DrawLine(spriteBatch, roomCenter(14), roomCenter(15));
            DrawLine(spriteBatch, roomCenter(15), roomCenter(16));
            DrawLine(spriteBatch, roomCenter(16), roomCenter(17));
            DrawLine(spriteBatch, roomCenter(17), roomCenter(18));
            DrawLine(spriteBatch, roomCenter(18), roomCenter(19));
            DrawLine(spriteBatch, roomCenter(19), roomCenter(20));
            DrawLine(spriteBatch, roomCenter(20), roomCenter(16));

            for (int i = 1; i < roomVectors.Length; i++)
            {
                if (i != gameBoard.player.pos)
                    spriteBatch.Draw(roomDoor, roomVectors[i], Color.LightSkyBlue);
                else
                    spriteBatch.Draw(roomDoor, roomVectors[i], Color.Red);
                spriteBatch.DrawString(courierFont, "" + i, roomCenter(i) - new Vector2(7, 10), Color.White);
            }
            spriteBatch.Draw(arrow, arrowVector, Color.White);
            spriteBatch.DrawString(courierFont, "" + gameBoard.player.arrows, arrowVector + new Vector2(arrow.Width / 2, arrow.Height + 10), Color.White);
        }
        void DrawLine(SpriteBatch batch, Vector2 start, Vector2 end)
        {
            Vector2 edge = end - start;
            // calculate angle
            float angle = (float)Math.Atan2(edge.Y, edge.X);

            batch.Draw(lineTexture, new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), 15), //width of line
                null,
                Color.White,
                angle,     //angle of line
                new Vector2(0, 0), // point in line about which to rotate
                SpriteEffects.None,
                0);
        }
        void DrawArrow(SpriteBatch batch, Vector2 start, Vector2 end)
        {
            Vector2 edge = end - start;
            // calculate angle
            float angle = (float)Math.Atan2(edge.Y, edge.X);

            batch.Draw(lineTexture, new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), 15), //width of line
                null,
                Color.Orange,
                angle,     //angle of line
                new Vector2(0, 0), // point in line about which to rotate
                SpriteEffects.None,
                0);
        }
        Vector2 roomCenter(int room)
        {
            return roomVectors[room] + new Vector2(roomDoor.Width / 2, roomDoor.Height / 2);
        }
    }
}
