using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace The_Ashenveil_Murders.Core.Entities
{
    public class Player
    {
        private Texture2D _texture;
        private Vector2 _position;
        private float _speed = 200f;
        public Player(Texture2D texture2D, Vector2 vector2)
        {
            _texture = texture2D;
            _position = vector2;
        }
        public void Update(GameTime gameTime)
        {
            var kb = Keyboard.GetState();
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (kb.IsKeyDown(Keys.Right)) _position.X += _speed * delta;
            if (kb.IsKeyDown(Keys.Left))  _position.X -= _speed * delta;
            if (kb.IsKeyDown(Keys.Up))    _position.Y -= _speed * delta;
            if (kb.IsKeyDown(Keys.Down))  _position.Y += _speed * delta;
        }
        public void draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            Rectangle shape = new Rectangle((int)_position.X, (int)_position.Y, 50, 50);
            //(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
            spriteBatch.Draw(_texture, _position, shape, Color.Red);
        }
    }
}