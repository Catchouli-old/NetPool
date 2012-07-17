using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Pool
{
    public class BallConfigurations
    {
        private BallConfigurations() { }

        private static string[] ballColours = new string[] { "red", "red", "orange", "orange", "black", "red", "red", "orange", "red", "orange", "orange", "orange", "red", "orange", "red" };

        public static List<Ball> Triangle(Game game, Texture2D ballTexture, Rectangle playableArea, int balls, Vector2 cueBallPosition, Vector2 firstBallPosition, int radius, string[] ballColours = null)
        {
            List<Ball> triangle = new List<Ball>();

            if (ballColours == null)
            {
                ballColours = BallConfigurations.ballColours;
            }

            // Account for cueball
            balls--;
            if (balls < 3)
                balls = 3;

            int diameter = 2 * radius;

            // Add cue ball
            triangle.Add(new Ball(game, ballTexture, cueBallPosition, "white", playableArea));

            int currentRow = 0;
            int current = 0;
            for (int i = 0; i < balls; i++)
            {
                if (current <= currentRow)
                {
                    float xchange = currentRow * diameter;
                    float ychange = current * diameter - currentRow * radius;
                    triangle.Add(new Ball(game, ballTexture, firstBallPosition + new Vector2(currentRow * diameter, current * diameter - currentRow * radius), ballColours[i % ballColours.Length], playableArea));
                    current++;
                }
                if (current > currentRow)
                {
                    current = 0;
                    currentRow++;
                }
            }

            return triangle;
        }
    }
}
