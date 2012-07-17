using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;

namespace Pool
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Pool : Microsoft.Xna.Framework.Game
    {
        public GraphicsDeviceManager graphics;
        public SpriteBatch spriteBatch;

        public bool ended = false;

        public Color background = Color.Black;

        // Ball holder
        public List<Ball> balls;
        // This is used for displaying which balls have been potted
        public List<Ball> pottedBalls;

        const int refugeX = 705;

        // State Variables
        public GameState _gameState;
        public MenuState _menuState;
        public GameMode _gameMode;
        public PlayerIndex _currentPlayer;
        Color _playerOneColour;
        Color _playerTwoColour;
        Color _firstColourHit;
        TurnState _turnState;
        bool _cueVisible;
        bool _freezeInput;
        bool _freezeInputPrevious;
        PlayerIndex _winningPlayer;
        int _winningTimer;
        int _cueBallTimer;
        // Multiplayer only
        public Networking _connection;
        public IPAddress _connectionAddress;
        public PlayerIndex _myPlayer;
        string _ipInput = "127.0.0.1";
        long _startTime = 0;
        long _lastInput = 0;

        // Resources
        Texture2D _fourpx;
        Texture2D _cue;
        Texture2D _cueball;
        Texture2D _table;
        Texture2D _darkenOverlay;
        Texture2D _logo;
        Texture2D _barUnderlay;
        Texture2D _barOverlay;

        // Menu buttons and text
        Texture2D[] _menuPlay = new Texture2D[3];
        Texture2D[] _menuPractice = new Texture2D[3];
        Texture2D[] _menuOffline = new Texture2D[3];
        Texture2D[] _menuHost = new Texture2D[3];
        Texture2D[] _menuConnect = new Texture2D[3];
        Texture2D[] _menuOk = new Texture2D[3];
        Texture2D[] _menuBack = new Texture2D[3];
        Texture2D _menuWaitingStatic;
        Texture2D _menuEnterIpStatic;
        Texture2D _explanation;

        // Fonts
        SpriteFont _spriteFont;

        // SoundEffect
        public SoundEffect _cueStrike;
        public SoundEffect _ballStrike;
        public SoundEffect _railBounce;
        public SoundEffect _pocketBounce;
        public SoundEffect _portal;
        
        // A semi-transparent colour for our ball predictors
        Color _semiTransparent;

        // Position and size
        Rectangle _tablePosition;
        Rectangle _playableArea;

        // Cue information
        public Vector2 _cuePosition;
        public double _cueRotation;
        public double _cueDistance;

        // Constants
        const int MAX_POWER = 150;

        // Ball Predictor
        BallPredictor _ballPredictor;

        // Stuff for showing some big text stuff
        Color _bigTextColour = Color.HotPink;
        string _bigtextString = "";
        SpriteFont _bigTextFont;
        int _bigTextTimer = 0;

        // Previous mouse and keyboard states
        Vector2 _mousePosition;
        MouseState _mouseStatePrevious;
        KeyboardState _keyboardStatePrevious;
        // The mouse position when the cue last started to be dragged
        Vector2 _lastMousePosition;

        public Pool()
        {
            _currentPlayer = PlayerIndex.One;
            _turnState = TurnState.SHOOTING;
            _gameState = GameState.MENU;
            _menuState = MenuState.HOME;
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            IsFixedTimeStep = true;
            TargetElapsedTime = new TimeSpan(0, 0, 0, 0, 10);
            IsMouseVisible = true;
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
            _mousePosition = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
            _lastMousePosition = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
            _keyboardStatePrevious = Keyboard.GetState();
            _mouseStatePrevious = Mouse.GetState();
            _playableArea = new Rectangle(85, 85, 1105, 555);
            _cueVisible = true;

            base.Initialize();
        }

        protected void PostLoadInitialize()
        {
            double scaleFactor = (double)graphics.PreferredBackBufferWidth / (double)_table.Width;
            _tablePosition = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, (int)Math.Round(scaleFactor * _table.Height));

            _ballPredictor = new BallPredictor(this, _cueball, _playableArea, _semiTransparent);
            InitializeGame(false);
        }

        public void InitializeGame(bool multiPlayer)
        {
            if (!multiPlayer)
            {
                GenerateBalls();
                pottedBalls = new List<Ball>();
                _winningPlayer = PlayerIndex.Three;
                _playerOneColour = Color.Transparent;
                _playerTwoColour = Color.Transparent;
            }
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load the required resources
            _fourpx = Content.Load<Texture2D>("4px");
            _cue = Content.Load<Texture2D>("cue");
            _cueball = Content.Load<Texture2D>("cueball");
            _table = Content.Load<Texture2D>("pooltable");
            _logo = Content.Load<Texture2D>("logo");
            _barUnderlay = Content.Load<Texture2D>("barunderlay");
            _barOverlay = Content.Load<Texture2D>("baroverlay");
            _darkenOverlay = Content.Load<Texture2D>("darken");

            // Menu buttons
            _menuPlay[0] = Content.Load<Texture2D>("menu_play_up");
            _menuPlay[1] = Content.Load<Texture2D>("menu_play_over");
            _menuPlay[2] = Content.Load<Texture2D>("menu_play_down");
            _menuPractice[0] = Content.Load<Texture2D>("menu_practice_up");
            _menuPractice[1] = Content.Load<Texture2D>("menu_practice_over");
            _menuPractice[2] = Content.Load<Texture2D>("menu_practice_down");
            _menuOffline[0] = Content.Load<Texture2D>("menu_offline_up");
            _menuOffline[1] = Content.Load<Texture2D>("menu_offline_over");
            _menuOffline[2] = Content.Load<Texture2D>("menu_offline_down");
            _menuHost[0] = Content.Load<Texture2D>("menu_host_up");
            _menuHost[1] = Content.Load<Texture2D>("menu_host_over");
            _menuHost[2] = Content.Load<Texture2D>("menu_host_down");
            _menuConnect[0] = Content.Load<Texture2D>("menu_connect_up");
            _menuConnect[1] = Content.Load<Texture2D>("menu_connect_over");
            _menuConnect[2] = Content.Load<Texture2D>("menu_connect_down");
            _menuOk[0] = Content.Load<Texture2D>("menu_ok_up");
            _menuOk[1] = Content.Load<Texture2D>("menu_ok_over");
            _menuOk[2] = Content.Load<Texture2D>("menu_ok_down");
            _menuBack[0] = Content.Load<Texture2D>("menu_back_up");
            _menuBack[1] = Content.Load<Texture2D>("menu_back_over");
            _menuBack[2] = Content.Load<Texture2D>("menu_back_down");
            // Menu text
            _menuWaitingStatic = Content.Load<Texture2D>("menu_awaiting_connection_static");
            _menuEnterIpStatic = Content.Load<Texture2D>("menu_enter_ip_static");
            _explanation = Content.Load<Texture2D>("explanation");

            // Make a transparent colour
            _semiTransparent = new Color(255, 255, 255, 100);

            // Load some sounds
            _cueStrike = Content.Load<SoundEffect>("cuestrike");
            _ballStrike = Content.Load<SoundEffect>("ballstrike");
            _railBounce = Content.Load<SoundEffect>("railbounce");
            _pocketBounce = Content.Load<SoundEffect>("pocketbounce");
            _portal = Content.Load<SoundEffect>("portal");

            // Load some fonts
            _spriteFont = Content.Load<SpriteFont>("SpriteFont1");
            _bigTextFont = Content.Load<SpriteFont>("BigFont");

            PostLoadInitialize();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            _mousePosition.X = Mouse.GetState().X;
            _mousePosition.Y = Mouse.GetState().Y;

            switch (GameState)
            {
                case GameState.MENU:
                    if ((GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
                        || (!Keyboard.GetState().IsKeyDown(Keys.Escape) && _keyboardStatePrevious.IsKeyDown(Keys.Escape)))
                        && _menuState != MenuState.AWAITCONNECTION)
                        EndGame();
                    UpdateMenu(gameTime);
                    break;
                case GameState.GAMEPLAY:
                    UpdateGame(gameTime);
                    if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
                        || (!Keyboard.GetState().IsKeyDown(Keys.Escape) && _keyboardStatePrevious.IsKeyDown(Keys.Escape)))
                        _gameState = GameState.MENU;
                    break;
                default:
                    break;
            }


            _mouseStatePrevious = Mouse.GetState();
            _keyboardStatePrevious = Keyboard.GetState();

            base.Update(gameTime);
        }

        protected void UpdateMenu(GameTime gameTime)
        {
            if (Mouse.GetState().LeftButton == ButtonState.Released && Mouse.GetState().LeftButton != _mouseStatePrevious.LeftButton)
            {
                switch (_menuState)
                {
                    case MenuState.HOME:
                        if (Mouse.GetState().Y >= 350 && Mouse.GetState().Y < 400)
                        {
                            _menuState = MenuState.MODE;
                        }
                        if (Mouse.GetState().Y >= 400 && Mouse.GetState().Y < 450)
                        {
                            _gameMode = GameMode.PRACTICE;
                            pottedBalls.Clear();
                            GenerateBalls();
                            _startTime = (long)gameTime.TotalGameTime.TotalSeconds;
                            _menuState = MenuState.HOME;
                            _gameState = GameState.GAMEPLAY;
                        }
                        break;
                    case MenuState.MODE:
                        if (Mouse.GetState().Y >= 350 && Mouse.GetState().Y < 400)
                        {
                            _gameMode = GameMode.OFFLINE;
                            GenerateBalls();
                            pottedBalls.Clear();
                            _menuState = MenuState.HOME;
                            _gameState = GameState.GAMEPLAY;
                        }
                        if (Mouse.GetState().Y >= 400 && Mouse.GetState().Y < 450)
                        {
                            _menuState = MenuState.AWAITCONNECTION;
                        }
                        if (Mouse.GetState().Y >= 450 && Mouse.GetState().Y < 500)
                        {
                            _menuState = MenuState.ENTERIP;
                        }
                        if (Mouse.GetState().Y >= 500 && Mouse.GetState().Y < 550)
                        {
                            _menuState = MenuState.HOME;
                        }
                        break;
                    case MenuState.AWAITCONNECTION:
                        if (Mouse.GetState().Y >= 450 && Mouse.GetState().Y < 500)
                        {
                            _menuState = MenuState.MODE;
                        }
                        break;
                    case MenuState.ENTERIP:
                        if (Mouse.GetState().Y >= 500 && Mouse.GetState().Y < 550)
                        {
                            if (IPAddress.TryParse(_ipInput, out _connectionAddress))
                                _menuState = MenuState.CONNECTING;
                        }
                        if (Mouse.GetState().Y >= 550 && Mouse.GetState().Y < 600)
                        {
                            _menuState = MenuState.MODE;
                        }
                        break;
                    default:
                        break;
                }
            }

            if (_menuState == MenuState.ENTERIP)
            {
                if (gameTime.TotalGameTime.TotalMilliseconds >= _lastInput + 150)
                {
                    if (Keyboard.GetState().IsKeyDown(Keys.D0) || Keyboard.GetState().IsKeyDown(Keys.NumPad0))
                    {
                        _ipInput += "0";
                        _lastInput = (long)gameTime.TotalGameTime.TotalMilliseconds;
                    }
                    if (Keyboard.GetState().IsKeyDown(Keys.D1) || Keyboard.GetState().IsKeyDown(Keys.NumPad1))
                    {
                        _ipInput += "1";
                        _lastInput = (long)gameTime.TotalGameTime.TotalMilliseconds;
                    }
                    if (Keyboard.GetState().IsKeyDown(Keys.D2) || Keyboard.GetState().IsKeyDown(Keys.NumPad2))
                    {
                        _ipInput += "2";
                        _lastInput = (long)gameTime.TotalGameTime.TotalMilliseconds;
                    }
                    if (Keyboard.GetState().IsKeyDown(Keys.D3) || Keyboard.GetState().IsKeyDown(Keys.NumPad3))
                    {
                        _ipInput += "3";
                        _lastInput = (long)gameTime.TotalGameTime.TotalMilliseconds;
                    }
                    if (Keyboard.GetState().IsKeyDown(Keys.D4) || Keyboard.GetState().IsKeyDown(Keys.NumPad4))
                    {
                        _ipInput += "4";
                        _lastInput = (long)gameTime.TotalGameTime.TotalMilliseconds;
                    }
                    if (Keyboard.GetState().IsKeyDown(Keys.D5) || Keyboard.GetState().IsKeyDown(Keys.NumPad5))
                    {
                        _ipInput += "5";
                        _lastInput = (long)gameTime.TotalGameTime.TotalMilliseconds;
                    }
                    if (Keyboard.GetState().IsKeyDown(Keys.D6) || Keyboard.GetState().IsKeyDown(Keys.NumPad6))
                    {
                        _ipInput += "6";
                        _lastInput = (long)gameTime.TotalGameTime.TotalMilliseconds;
                    }
                    if (Keyboard.GetState().IsKeyDown(Keys.D7) || Keyboard.GetState().IsKeyDown(Keys.NumPad7))
                    {
                        _ipInput += "7";
                        _lastInput = (long)gameTime.TotalGameTime.TotalMilliseconds;
                    }
                    if (Keyboard.GetState().IsKeyDown(Keys.D8) || Keyboard.GetState().IsKeyDown(Keys.NumPad8))
                    {
                        _ipInput += "8";
                        _lastInput = (long)gameTime.TotalGameTime.TotalMilliseconds;
                    }
                    if (Keyboard.GetState().IsKeyDown(Keys.D9) || Keyboard.GetState().IsKeyDown(Keys.NumPad9))
                    {
                        _ipInput += "9";
                        _lastInput = (long)gameTime.TotalGameTime.TotalMilliseconds;
                    }
                    if (Keyboard.GetState().IsKeyDown(Keys.OemPeriod))
                    {
                        _ipInput += ".";
                        _lastInput = (long)gameTime.TotalGameTime.TotalMilliseconds;
                    }
                    if (Keyboard.GetState().IsKeyDown(Keys.OemSemicolon))
                    {
                        _ipInput += ":";
                        _lastInput = (long)gameTime.TotalGameTime.TotalMilliseconds;
                    }
                    if (Keyboard.GetState().IsKeyDown(Keys.Back) && _ipInput.Length > 0)
                    {
                        _ipInput = _ipInput.Substring(0, _ipInput.Length - 1);
                        _lastInput = (long)gameTime.TotalGameTime.TotalMilliseconds;
                    }
                    if (!Keyboard.GetState().IsKeyDown(Keys.Enter) && _keyboardStatePrevious.IsKeyDown(Keys.Enter))
                    {
                        if (IPAddress.TryParse(_ipInput, out _connectionAddress))
                        {
                            _menuState = MenuState.CONNECTING;
                        }
                    }
                }
            }
            else if (_menuState == MenuState.CONNECTING)
            {
                if (_connection == null)
                {
                    ConnectionExitedDelegate ced = new ConnectionExitedDelegate(ConnectionExited);
                    _connection = new Networking(this, ced, _connectionAddress, 7167, false);
                }
                else if (_connection.Connected)
                {
                    _gameState = GameState.GAMEPLAY;
                    pottedBalls.Clear();
                    _gameMode = GameMode.ONLINE;
                    _myPlayer = PlayerIndex.Two;
                    GenerateBalls();
                    _menuState = MenuState.HOME;
                }
            }
            else if (_menuState == MenuState.AWAITCONNECTION)
            {
                if (_connection == null)
                {
                    ConnectionExitedDelegate ced = new ConnectionExitedDelegate(ConnectionExited);
                    _connection = new Networking(this, ced, IPAddress.Any, 7167, true);
                }
                else if (_connection.Connected)
                {
                    _gameState = GameState.GAMEPLAY;
                    _gameMode = GameMode.ONLINE;
                    _myPlayer = PlayerIndex.One;
                    GenerateBalls();
                    _menuState = MenuState.HOME;
                }
            }
        }

        protected void UpdateGame(GameTime gameTime)
        {
            #region TEST
            //if (Mouse.GetState().LeftButton == ButtonState.Pressed && Mouse.GetState().LeftButton != _mouseStatePrevious.LeftButton)
            //{
            //    testRectangle.X = Mouse.GetState().X;
            //    testRectangle.Y = Mouse.GetState().Y;
            //}
            //else if (Mouse.GetState().LeftButton == ButtonState.Pressed)
            //{
            //    testRectangle.Width = Mouse.GetState().X - testRectangle.X;
            //    testRectangle.Height = Mouse.GetState().Y - testRectangle.Y;
            //}
            //if (Keyboard.GetState().IsKeyDown(Keys.Left) && Keyboard.GetState().IsKeyDown(Keys.Left) != _keyboardStatePrevious.IsKeyDown(Keys.Left))
            //{
            //    spherePosition.X--;
            //}
            //if (Keyboard.GetState().IsKeyDown(Keys.Right) && Keyboard.GetState().IsKeyDown(Keys.Right) != _keyboardStatePrevious.IsKeyDown(Keys.Right))
            //{
            //    spherePosition.X++;
            //}
            //if (Keyboard.GetState().IsKeyDown(Keys.Down) && Keyboard.GetState().IsKeyDown(Keys.Down) != _keyboardStatePrevious.IsKeyDown(Keys.Down))
            //{
            //    spherePosition.Y++;
            //}
            //if (Keyboard.GetState().IsKeyDown(Keys.Up) && Keyboard.GetState().IsKeyDown(Keys.Up) != _keyboardStatePrevious.IsKeyDown(Keys.Up))
            //{
            //    spherePosition.Y--;
            //}
            #endregion

            if (_bigTextTimer > 0)
            {
                _bigTextTimer -= gameTime.ElapsedGameTime.Milliseconds;
            }

            if (_cueBallTimer > 0)
            {
                _cueBallTimer -= gameTime.ElapsedGameTime.Milliseconds;
                _freezeInput = true;
            }
            else
            {
                /// TODO LOL
                if (_cueBallTimer < 0)
                    _cueBallTimer = 0;
            }

            if (_gameMode == GameMode.ONLINE && _currentPlayer != _myPlayer)
            {
                _freezeInput = true;
                _cueVisible = false;
            }
            else
            {
                _cueVisible = true;
            }

            if (!_freezeInput)
            {
                _cuePosition = balls[0].Position;
                if (Mouse.GetState().LeftButton == ButtonState.Released)
                {
                    _lastMousePosition = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
                    if (_mouseStatePrevious.LeftButton == ButtonState.Pressed && _cueDistance > 0)
                    {
                        // do shot
                        MakeShot(_cueDistance, _cueRotation, false);
                        if (_gameMode == GameMode.ONLINE && _connection != null)
                        {
                            _connection.SendSync(balls);
                            _connection.SendVelocity(_cueDistance, _cueRotation);
                        }
                        _cueDistance = 0;
                    }
                    else
                    {
                        _cueRotation = Math.Atan2(Mouse.GetState().X - balls[0].X, Mouse.GetState().Y - balls[0].Y);
                    }
                }
                else if (Mouse.GetState().LeftButton == ButtonState.Pressed && _mouseStatePrevious.LeftButton == ButtonState.Released)
                {
                    _lastMousePosition = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
                }
                else
                {
                    double mouseDistanceFromCue = (_mousePosition - balls[0].Position).Length();

                    // let the user pull the cue back to decide power
                    // (let me overcomplicate everything for you!)
                    // Angles between the polar axes centred around the ball and various lines
                    double lastMousePosAngle = _cueRotation;
                    double currentMousePosAngle = Math.Atan2(Mouse.GetState().X - balls[0].X, Mouse.GetState().Y - balls[0].Y);
                    // A triangle constructed of:
                    // - The line between the ball and where the mouse currently is
                    // - The line between the ball and where the mouse was when lmb was first held
                    // - The third line connecting these two lines, creating a right angle between this line and the line to where lmb was first held
                    // The angle is gotten by subtracting the angles above
                    double theta = currentMousePosAngle - lastMousePosAngle;
                    // The length of the first line segment
                    double firstLineSegment = (_mousePosition - balls[0].Position).Length();
                    // Then use trigonometry to calculate the length of the third line
                    double trigThirdLine = firstLineSegment * Math.Cos(theta);
                    double fullLine = (balls[0].Position - _lastMousePosition).Length();
                    // And subtract this length from line segment 2 to get the distance of the mouse from that point
                    _cueDistance = (int)Math.Floor(fullLine - trigThirdLine);

                    if (_cueDistance < 0)
                        _cueDistance = 0;
                    if (_cueDistance > MAX_POWER)
                        _cueDistance = MAX_POWER;
                }

                if (_cueDistance > 0 && Mouse.GetState().LeftButton == ButtonState.Released)
                    _cueDistance = 0;

                _cuePosition.X = balls[0].Position.X - (float)(Math.Sin(_cueRotation) * _cueDistance);
                _cuePosition.Y = balls[0].Position.Y - (float)(Math.Cos(_cueRotation) * _cueDistance);
            }

            _ballPredictor.Update(gameTime, balls, _cueRotation);

            foreach (Ball ball in balls)
            {
                foreach (Ball other in balls)
                {
                    if (ball != other)
                    {
                        if (ball.Velocity.LengthSquared() > 0)
                        {
                            if (ball.BoundingSphere.Intersects(other.BoundingSphere))
                            {
                                Vector2 impact = other.Velocity - ball.Velocity;
                                Vector2 impulse = Vector2.Normalize(other.Position - ball.Position);
                                float impactSpeed = Vector2.Dot(impact, impulse);
                                if (impactSpeed < -2000)
                                    impactSpeed = -2000;
                                if (impactSpeed < 0)
                                {
                                    _ballStrike.Play(impactSpeed / -2000.0f, 0.0f, 0.0f);
                                    if (ball.Colour != Color.White && other.Colour != Color.White)
                                    {
                                        Color tempColour = ball.Colour;
                                        ball.Colour = other.Colour;
                                        other.Colour = tempColour;
                                    }
                                    else if (ball.Colour == Color.White)
                                    {
                                        if (_firstColourHit == Color.Transparent)
                                        {
                                            _firstColourHit = other.Colour;
                                            //string colourHit;
                                            //if (other.Colour == Color.Black)
                                            //    colourHit = "black";
                                            //else if (other.Colour == Color.Orange)
                                            //    colourHit = "orange";
                                            //else if (other.Colour == Color.Red)
                                            //    colourHit = "red";
                                            //else
                                            //    colourHit = "BAD";
                                            //ShowBigText(Color.White, "Hit " + colourHit, 1500);
                                        }

                                    }
                                    else if (other.Colour == Color.White)
                                    {
                                        if (_firstColourHit == Color.Transparent)
                                        {
                                            _firstColourHit = ball.Colour;
                                            //string colourHit;
                                            //if (ball.Colour == Color.Black)
                                            //    colourHit = "black";
                                            //else if (ball.Colour == Color.Orange)
                                            //    colourHit = "orange";
                                            //else if (ball.Colour == Color.Red)
                                            //    colourHit = "red";
                                            //else
                                            //    colourHit = "BAD";
                                        }
                                    }

                                    impulse *= impactSpeed * 0.95f;
                                    other.Velocity -= impulse;
                                    ball.Velocity += impulse;
                                }
                            }
                        }
                    }
                }

                ball.Update(gameTime);
            }

            for (int i = 0; i < pottedBalls.Count; i++)
            {
                if (pottedBalls[i].X > refugeX + (i * 30))
                {
                    pottedBalls[i].X -= (float)(0.5 * gameTime.ElapsedGameTime.Milliseconds);
                }
                if (pottedBalls[i].X < refugeX + (i * 30))
                {
                    pottedBalls[i].X = refugeX + (i * 30);
                }
            }

            _freezeInput = false;
            for (int i = 0; i < balls.Count; i++)
            {
                if (balls[i]._potted)
                {
                    // If it aint the cue ball.. damnit
                    if (balls[i].Colour != Color.White)
                    {
                        _pocketBounce.Play(1.0f, 0.0f, 0.0f);
                        // If it aint the black.. double damnit
                        if (balls[i].Colour != Color.Black)
                        {
                            // If players are not yet assigned colours
                            if (_playerOneColour == Color.Transparent)
                            {
                                // Assign 'em I guess
                                Color otherColour;
                                if (balls[i].Colour == Color.Red)
                                    otherColour = Color.Orange;
                                else
                                    otherColour = Color.Red;

                                switch (_currentPlayer)
                                {
                                    case PlayerIndex.One:
                                        _playerOneColour = balls[i].Colour;
                                        _playerTwoColour = otherColour;
                                        break;
                                    case PlayerIndex.Two:
                                        _playerOneColour = otherColour;
                                        _playerTwoColour = balls[i].Colour;
                                        break;
                                }

                                ShowBigText(Color.White, "SCORE", 1500);
                                _turnState = TurnState.SCORED;
                            }
                            else
                            {
                                // Otherwise check if they potted their own ball
                                Color currentPlayersColour;
                                if (_currentPlayer == PlayerIndex.One)
                                {
                                    currentPlayersColour = _playerOneColour;
                                }
                                else
                                {
                                    currentPlayersColour = _playerTwoColour;
                                }
                                if (balls[i].Colour == currentPlayersColour)
                                {
                                    // BAM
                                    _turnState = TurnState.SCORED;
                                    ShowBigText(Color.White, "SCORE", 1500);
                                }
                            }
                        }
                        else
                        {
                            // If they pot the black
                            Color currentPlayerColour;
                            if (_currentPlayer == PlayerIndex.One)
                                currentPlayerColour = _playerOneColour;
                            else
                                currentPlayerColour = _playerTwoColour;

                            int currentPlayerBallsLeft = 0;
                            foreach (Ball ball in balls)
                            {
                                if (ball.Colour == currentPlayerColour)
                                    currentPlayerBallsLeft++;
                            }

                            // If they have more than one ball of their colour left, fuck
                            if (currentPlayerBallsLeft > 0 || currentPlayerColour == Color.Transparent || (_firstColourHit != currentPlayerColour && _firstColourHit != Color.Black))
                            {
                                PlayerIndex otherPlayer;
                                if (_currentPlayer == PlayerIndex.One)
                                    otherPlayer = PlayerIndex.Two;
                                else
                                    otherPlayer = PlayerIndex.One;

                                _winningPlayer = otherPlayer;
                                _winningTimer = 14000;
                                _freezeInput = true;
                            }
                            else
                            {
                                _winningPlayer = _currentPlayer;
                                _winningTimer = 14000;
                                _freezeInput = true;
                            }
                        }
                        pottedBalls.Add(balls[i]);
                        balls[i].X = 2000;
                        balls[i].Y = 676;
                        balls.Remove(balls[i]);
                        i--;
                    }
                    else
                    {
                        // If white,
                        _portal.Play(1.0f, 1.0f, 1.0f);
                        balls[i]._potted = false;
                        _freezeInput = true;
                        _cueBallTimer = 1000;
                        balls[i].Position = Ball._reversedPocketSpheres[balls[i]._pocketCollided];
                    }
                }
                else if (balls[i].Velocity.LengthSquared() > 0)
                {
                    _freezeInput = true;
                }
            }

            if (_winningTimer > 0)
            {
                _freezeInput = true;
                _winningTimer -= gameTime.ElapsedGameTime.Milliseconds;
            }
            else if (_winningPlayer != PlayerIndex.Three)
            {
                // Restart game when timer gets to 0
                _winningTimer = 0;
                GenerateBalls();
                _playerOneColour = Color.Transparent;
                _playerTwoColour = Color.Transparent;
                _startTime = gameTime.TotalGameTime.Seconds;
                InitializeGame(false);
            }

            if (_freezeInput == false && _freezeInputPrevious == true && _gameMode != GameMode.PRACTICE)
            {
                // Play just resumed, then
                Color currentPlayerColour;
                if (_currentPlayer == PlayerIndex.One)
                    currentPlayerColour = _playerOneColour;
                else
                    currentPlayerColour = _playerTwoColour;

                // Check how many balls the current player has
                int currentPlayerBallsLeft = 0;
                foreach (Ball ball in balls)
                {
                    if (ball.Colour == currentPlayerColour)
                        currentPlayerBallsLeft++;
                }

                if ((currentPlayerColour == Color.Transparent && (_firstColourHit == Color.Red || _firstColourHit == Color.Orange))
                    || (currentPlayerColour != Color.Transparent && _firstColourHit == currentPlayerColour)
                    || ((currentPlayerBallsLeft == 0 && currentPlayerColour != Color.Transparent) && _firstColourHit == Color.Black))
                {

                }
                else if (_winningTimer == 0)
                {
                    ShowBigText(Color.White, "Foul", 1500);
                }
                else if (_winningTimer != 0)
                {
                    _firstColourHit = Color.Transparent;
                }

                _firstColourHit = Color.Transparent;

                // Switch players
                if (_turnState != TurnState.SCORED)
                {
                    if (_currentPlayer == PlayerIndex.One)
                        _currentPlayer = PlayerIndex.Two;
                    else
                        _currentPlayer = PlayerIndex.One;
                }
                _turnState = TurnState.SHOOTING;
            }

            _freezeInputPrevious = _freezeInput;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(background);

            spriteBatch.Begin();

            switch (GameState)
            {
                case GameState.MENU:
                    DrawMenu(gameTime);
                    break;
                case GameState.GAMEPLAY:
                    DrawGame(gameTime);
                    break;
                default:
                    break;
            }
            
            //spriteBatch.DrawString(_spriteFont, _mousePosition.X.ToString() + "," + _mousePosition.Y.ToString(), new Vector2(_mousePosition.X + 50, _mousePosition.Y + 50), Color.Black);

            // Big text!
            if (_bigTextTimer > 0)
            {
                spriteBatch.DrawString(_bigTextFont, _bigtextString, new Vector2(640 - (10 * _bigtextString.Length), 300), _bigTextColour);
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }

        protected void DrawMenu(GameTime gameTime)
        {
            spriteBatch.Draw(_table, new Rectangle(0, 0, 1280, 720), Color.White);
            spriteBatch.Draw(_darkenOverlay, new Rectangle(0, 0, 1280, 720), Color.White);
            spriteBatch.Draw(_logo, new Vector2(150, 80), Color.White);
            spriteBatch.Draw(_explanation, new Vector2(830, 355), Color.White);

            switch (_menuState)
            {
                case MenuState.HOME:
                    if (Mouse.GetState().Y >= 350 && Mouse.GetState().Y < 400)
                    {
                        if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                            spriteBatch.Draw(_menuPlay[1], new Vector2(150, 350), Color.White);
                        else
                            spriteBatch.Draw(_menuPlay[2], new Vector2(150, 350), Color.White);
                    }
                    else
                    {
                        spriteBatch.Draw(_menuPlay[0], new Vector2(150, 350), Color.White);
                    }
                    if (Mouse.GetState().Y >= 400 && Mouse.GetState().Y < 450)
                    {
                        if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                            spriteBatch.Draw(_menuPractice[1], new Vector2(150, 400), Color.White);
                        else
                            spriteBatch.Draw(_menuPractice[2], new Vector2(150, 400), Color.White);
                    }
                    else
                    {
                        spriteBatch.Draw(_menuPractice[0], new Vector2(150, 400), Color.White);
                    }
                    break;
                case MenuState.MODE:
                    if (Mouse.GetState().Y >= 350 && Mouse.GetState().Y < 400)
                    {
                        if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                            spriteBatch.Draw(_menuOffline[1], new Vector2(150, 350), Color.White);
                        else
                            spriteBatch.Draw(_menuOffline[2], new Vector2(150, 350), Color.White);
                    }
                    else
                    {
                        spriteBatch.Draw(_menuOffline[0], new Vector2(150, 350), Color.White);
                    }
                    if (Mouse.GetState().Y >= 400 && Mouse.GetState().Y < 450)
                    {
                        if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                            spriteBatch.Draw(_menuHost[1], new Vector2(150, 400), Color.White);
                        else
                            spriteBatch.Draw(_menuHost[2], new Vector2(150, 400), Color.White);
                    }
                    else
                    {
                        spriteBatch.Draw(_menuHost[0], new Vector2(150, 400), Color.White);
                    }

                    if (Mouse.GetState().Y >= 450 && Mouse.GetState().Y < 500)
                    {
                        if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                            spriteBatch.Draw(_menuConnect[1], new Vector2(150, 450), Color.White);
                        else
                            spriteBatch.Draw(_menuConnect[2], new Vector2(150, 450), Color.White);
                    }
                    else
                    {
                        spriteBatch.Draw(_menuConnect[0], new Vector2(150, 450), Color.White);
                    }

                    if (Mouse.GetState().Y >= 500 && Mouse.GetState().Y < 550)
                    {
                        if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                            spriteBatch.Draw(_menuBack[1], new Vector2(150, 500), Color.White);
                        else
                            spriteBatch.Draw(_menuBack[2], new Vector2(150, 500), Color.White);
                    }
                    else
                    {
                        spriteBatch.Draw(_menuBack[0], new Vector2(150, 500), Color.White);
                    }
                    break;
                case MenuState.AWAITCONNECTION:
                    spriteBatch.Draw(_menuWaitingStatic, new Vector2(150, 350), Color.White);

                    if (Mouse.GetState().Y >= 450 && Mouse.GetState().Y < 500)
                    {
                        if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                            spriteBatch.Draw(_menuBack[1], new Vector2(150, 450), Color.White);
                        else
                            spriteBatch.Draw(_menuBack[2], new Vector2(150, 450), Color.White);
                    }
                    else
                    {
                        spriteBatch.Draw(_menuBack[0], new Vector2(150, 450), Color.White);
                    }
                    break;
                case MenuState.ENTERIP:
                    spriteBatch.Draw(_menuEnterIpStatic, new Vector2(150, 350), Color.White);

                    if (Mouse.GetState().Y >= 500 && Mouse.GetState().Y < 550)
                    {
                        if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                            spriteBatch.Draw(_menuOk[1], new Vector2(150, 500), Color.White);
                        else
                            spriteBatch.Draw(_menuOk[2], new Vector2(150, 500), Color.White);
                    }
                    else
                    {
                        spriteBatch.Draw(_menuOk[0], new Vector2(150, 500), Color.White);
                    }

                    if (Mouse.GetState().Y >= 550 && Mouse.GetState().Y < 600)
                    {
                        if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                            spriteBatch.Draw(_menuBack[1], new Vector2(150, 550), Color.White);
                        else
                            spriteBatch.Draw(_menuBack[2], new Vector2(150, 550), Color.White);
                    }
                    else
                    {
                        spriteBatch.Draw(_menuBack[0], new Vector2(150, 550), Color.White);
                    }

                    spriteBatch.DrawString(_bigTextFont, _ipInput, new Vector2(295, 400), Color.White);
                    break;
                default:
                    break;
            }
        }

        protected void DrawGame(GameTime gameTime)
        {
            spriteBatch.Draw(_table, _tablePosition, Color.White);
            //spriteBatch.Draw(_fourpx, testRectangle, Color.White);
            //spriteBatch.DrawString(_spriteFont, testRectangle.X.ToString() + ", " + testRectangle.Y.ToString() + ", " + testRectangle.Width.ToString() + ", " + testRectangle.Height.ToString(), new Vector2(testRectangle.X - 25, testRectangle.Y - 25), Color.White);

            if (!InputFrozen)
            {
                _ballPredictor.Draw(gameTime);
            }

            // Draw balls
            foreach (Ball ball in balls)
            {
                ball.Draw(gameTime);
            }

            // Draw potted balls
            spriteBatch.Draw(_barUnderlay, new Vector2(0, 0), Color.White);
            for (int i = 0; i < pottedBalls.Count; i++)
            {
                if (pottedBalls[i].X <= 1125)
                    spriteBatch.Draw(_cueball, pottedBalls[i].Position, pottedBalls[i].Colour);
            }
            spriteBatch.Draw(_barOverlay, new Vector2(0, 0), Color.White);

            // Draw cue
            if (_cueVisible)
                spriteBatch.Draw(_cue, _cuePosition, null, Color.White, (float)-_cueRotation, new Vector2(8, 870 + (_cueball.Width / 2)), 1.0f, SpriteEffects.None, 1.0f);

            if (_gameMode != GameMode.PRACTICE)
            {
                string currentPlayer = "Current player: ";
                switch (_currentPlayer)
                {
                    case PlayerIndex.One:
                        currentPlayer += "One";
                        break;
                    case PlayerIndex.Two:
                        currentPlayer += "Two";
                        break;
                    default:
                        currentPlayer += "ERROR";
                        break;
                }
                spriteBatch.DrawString(_spriteFont, currentPlayer, new Vector2(25, 25), Color.White);
                string currentPlayerColour;
                if (_playerOneColour == Color.Transparent)
                {
                    currentPlayerColour = "None";
                }
                else if (_currentPlayer == PlayerIndex.One)
                {
                    currentPlayerColour = ColourToString(_playerOneColour);
                }
                else
                {
                    currentPlayerColour = ColourToString(_playerTwoColour);
                }

                spriteBatch.DrawString(_spriteFont, "Player's colour: " + currentPlayerColour, new Vector2(25, 50), Color.White);
            }
            else
            {
                long totalSeconds = (long)gameTime.TotalGameTime.TotalSeconds - _startTime;
                long seconds = (totalSeconds % 3600) % 60;
                long minutes = (totalSeconds / 60) % 60;
                long hours = (totalSeconds / 3600);
                string time = (hours < 10 ? "0" : "") + hours.ToString() + ":" + (minutes < 10 ? "0" : "") + minutes.ToString() + ":" + (seconds < 10 ? "0" : "") + seconds.ToString();
                spriteBatch.DrawString(_spriteFont, "Time: " + time, new Vector2(25, 25), Color.White);
            }

            if (_winningPlayer != PlayerIndex.Three)
            {
                string winningPlayerString;
                if (_winningPlayer == PlayerIndex.One)
                    winningPlayerString = "One";
                else
                    winningPlayerString = "Two";

                string winningString = (_gameMode != GameMode.PRACTICE ? "Player " + winningPlayerString + " wins!" : "Game over!") + " The Game restarts in " + ((_winningTimer + 1) / 1000) + " seconds.";
                spriteBatch.DrawString(_bigTextFont, winningString, new Vector2(600 - (10 * winningString.Length), 300), Color.White);
            }

#if (DEBUG)
            //foreach (Rectangle rect in Ball._pocketRectangles)
            //{
            //    spriteBatch.Draw(_fourpx, rect, Color.White);
            //}
            //spriteBatch.Draw(_circle, spherePosition, Color.AliceBlue);
#endif
        }

        public bool InputFrozen
        {
            get
            {
                return _freezeInput;
            }
            set
            {
                _freezeInput = value;
            }
        }

        public virtual GameState GameState
        {
            get
            {
                return _gameState;
            }
            set
            {
                _gameState = value;
            }
        }

        public virtual TurnState TurnState
        {
            get
            {
                return _turnState;
            }
            set
            {
                _turnState = value;
            }
        }

        string ColourToString(Color ballColour)
        {
            if (ballColour == Color.Red)
                return "Red";
            if (ballColour == Color.Orange)
                return "Orange";
            if (ballColour == Color.Black)
                return "Black";
            else
                return "White";
        }

        void ShowBigText(Color bigTextColour, string bigTextString, int bigTextTime)
        {
            _bigTextColour = bigTextColour;
            _bigtextString = bigTextString;
            _bigTextTimer = bigTextTime;
        }

        public void GenerateBalls()
        {
            balls = BallConfigurations.Triangle(this, _cueball, _playableArea, 16, new Vector2(360, 360), new Vector2(800, 360), _cueball.Width / 2);
        }

        public void ConnectionExited(ConnectionError e)
        {
            Console.WriteLine(e.ToString());
            if (_connection != null)
            {
                _connection.Close();
                _connection = null;
            }


            switch (e)
            {
                case ConnectionError.LISTENING_SOCKET_IN_USE:
                    this._gameState = GameState.MENU;
                    break;
                case ConnectionError.REJECTED_BY_HOST:
                    this._gameState = GameState.MENU;
                    break;
                case ConnectionError.CONNECTION_EXITED:
                    this._gameState = GameState.MENU;
                    break;
                default:
                    this._gameState = GameState.MENU;
                    break;
            }

            GenerateBalls();
        }

        public void MakeShot(double cueDistance, double cueRotation, bool remote)
        {
            balls[0].SetVelocity(cueDistance * 15, cueRotation);
            float volume = (float)(cueDistance / MAX_POWER);
            _cueStrike.Play(volume, 0.0f, 0.0f);
            _freezeInput = true;
        }

        public void EndGame()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection = null;
            }
            ended = true;
            this.Exit();
        }
    }

    public delegate void ConnectionExitedDelegate(ConnectionError e);

    public enum GameState
    {
        MENU,
        GAMEPLAY
    }

    public enum TurnState
    {
        SHOOTING,
        MADESHOT,
        SCORED
    }

    public enum MenuState
    {
        HOME,
        MODE,
        AWAITCONNECTION,
        ENTERIP,
        CONNECTING,
        PAUSE
    }

    public enum GameMode
    {
        PRACTICE,
        OFFLINE,
        ONLINE
    }
}
