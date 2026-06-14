using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace The_Ashenveil_Murders.Core.Screens
{
    public class Gamebox
    {
        private Texture2D _pixel;
        private int _gap;
        private int _thickness;
        public Rectangle Bounds { get; private set; }
        public Gamebox(Texture2D pixel, int screenWidth, int screenHeight, int gap = 20, int thickness = 4)
        {
            _pixel = pixel;
            _gap = gap;
            _thickness = thickness;

            // The playable area sits just inside the border
            Bounds = new Rectangle(
                gap + thickness,
                gap + thickness,
                screenWidth - (gap + thickness) * 2,
                screenHeight - (gap + thickness) * 2
            );
        }

        public void draw(SpriteBatch spriteBatch)
        {
            Color color = Color.Red;
            int w = Bounds.X + Bounds.Width + _thickness;
            int h = Bounds.Y + Bounds.Height + _thickness;
            //new Rectangle(x,  y,  width,  height )
            // Top
            spriteBatch.Draw(_pixel, new Rectangle(_gap, _gap, w - _gap * 2 + _thickness, _thickness), Color.White);
            // Bottom
            spriteBatch.Draw(_pixel, new Rectangle(_gap, h, w - _gap * 2 + _thickness, _thickness), Color.White);
            // Left
            spriteBatch.Draw(_pixel, new Rectangle(_gap, _gap, _thickness, h - _gap), Color.White);
            // Right
            spriteBatch.Draw(_pixel, new Rectangle(w, _gap, _thickness, h - _gap), Color.White);
        }
    }
}