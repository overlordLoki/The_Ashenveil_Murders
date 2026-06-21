using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using The_Ashenveil_Murders.Core.Screens;

namespace The_Ashenveil_Murders.Core
{
    public class The_Ashenveil_MurdersGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _pixel;
        private SpriteFont _font;
        private GameScreen _gameScreen;

        public static readonly bool IsMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
        public static readonly bool IsDesktop = OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

        public The_Ashenveil_MurdersGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Services.AddService(typeof(GraphicsDeviceManager), _graphics);
            Content.RootDirectory = "Content";

            _graphics.PreferredBackBufferWidth  = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;

            Window.AllowUserResizing = true;
            Window.Title = "The Ashenveil Murders";
            IsMouseVisible = true;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _font = Content.Load<SpriteFont>("Fonts/Hud");

            _gameScreen = new GameScreen(_pixel, _font, this);

            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
                || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            _gameScreen?.Update(gameTime, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();
            _gameScreen?.Draw(gameTime, _spriteBatch, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
