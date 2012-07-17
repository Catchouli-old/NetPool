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
    /*
     * Ball Predictor
     * Predicts balls
     * What did you think it did?
     * Author: Bacun
     */
    public class BallPredictor : DrawableGameComponent
    {
        Color _colour;
        List<Ball> _collidableBalls;
        Rectangle _playableArea;
        int _radius;
        Vector2 _radiusVector;
        Vector2 _position;
        Texture2D _texture;

        public BallPredictor(Game game, Texture2D texture, Rectangle playableArea, Color colour)
            : base(game)
        {
            _position = new Vector2(0, 0);
            _texture = texture;
            _colour = colour;
            _playableArea = playableArea;
            _radius = (int)Math.Floor(texture.Width / 2.0);
            _radiusVector = new Vector2(_radius, _radius);
            _collidableBalls = new List<Ball>();
        }

        public void Update(GameTime gameTime, List<Ball> balls, double rotation)
        {
            if (!((Pool)this.Game).InputFrozen)
            {
                Ball closeBall;
                _collidableBalls.Clear();
                foreach (Ball ball in balls)
                {
                    Vector2 direction = new Vector2((float)Math.Sin(rotation), (float)Math.Cos(rotation));
                    Vector2 lineOfSight = (ball.Position - balls[0].Position);
                    double dotProduct = Vector2.Dot(direction, lineOfSight);
                    double sumRadiiSquared;
                    double F;
                    if (dotProduct > 0)
                    {
                        sumRadiiSquared = balls[0].BoundingSphere.Radius + ball.BoundingSphere.Radius;
                        sumRadiiSquared *= sumRadiiSquared;

                        double lengthC = lineOfSight.Length();
                        F = (lengthC * lengthC) - (dotProduct * dotProduct);

                        if (F < sumRadiiSquared)
                        {
                            _collidableBalls.Add(ball);
                        }
                    }
                }

                if (_collidableBalls.Count > 0)
                {
                    closeBall = _collidableBalls[0];
                    foreach (Ball otherBall in _collidableBalls)
                    {
                        if (closeBall != otherBall)
                        {
                            if ((balls[0].Position - closeBall.Position).LengthSquared() > (balls[0].Position - otherBall.Position).LengthSquared())
                            {
                                closeBall = otherBall;
                            }
                        }
                    }

                    if ((closeBall.Position - balls[0].Position).LengthSquared() > 50 * 50)
                    {
                        Vector2 direction = new Vector2((float)Math.Sin(rotation), (float)Math.Cos(rotation));
                        Vector2 lineOfSight = (closeBall.Position - balls[0].Position);
                        double dotProduct = Vector2.Dot(direction, lineOfSight);
                        double sumRadiiSquared = balls[0].BoundingSphere.Radius + closeBall.BoundingSphere.Radius;
                        sumRadiiSquared *= sumRadiiSquared;
                        double lengthC = lineOfSight.Length();
                        double F = (lengthC * lengthC) - (dotProduct * dotProduct);

                        double T = sumRadiiSquared - F;

                        if (T >= 0)
                        {
                            double distance = dotProduct - Math.Sqrt(T);
                            direction *= (float)distance;

                            _position = balls[0].Position + direction;
                        }
                    }
                    else
                    {
                        _position.X = -_radius;
                        _position.Y = -_radius;
                    }
                }
                else
                {
                    /*double angle = (rotation < -MathHelper.PiOver2 || rotation >= MathHelper.PiOver2 ? MathHelper.Pi : 0) - rotation;
                    double adjacent = _playableArea.Y + (rotation < -MathHelper.PiOver2 || rotation >= MathHelper.PiOver2 ? 0 : _playableArea.Height) - balls[0].Position.Y;

                    if (Math.Cos(angle) != 0)
                    {
                        double distance = adjacent / Math.Cos(angle);
                        Vector2 moveVec = new Vector2((float)(Math.Sin(rotation) * distance), (float)(Math.Cos(rotation) * distance));
                        _position = balls[0].Position + (rotation < -MathHelper.PiOver2 || rotation >= MathHelper.PiOver2 ? -moveVec : moveVec) + (rotation < -MathHelper.PiOver2 || rotation >= MathHelper.PiOver2 ? _radiusVector : -_radiusVector);
                        if (_position.X < _playableArea.X + _radius)
                        {
                            angle = MathHelper.PiOver2 + rotation;
                            adjacent = _playableArea.X + (rotation < -MathHelper.PiOver2 || rotation >= MathHelper.PiOver2 ? 0 : _playableArea.Width) - balls[0].Position.X;
                            _position.X = _playableArea.X + _radius;
                            _position.Y = _playableArea.X + (float)(Math.Cos(rotation) * distance);
                        }
                        else if (_position.X > _playableArea.X + _playableArea.Width - _radius)
                        {
                            _position.X = _playableArea.X + _playableArea.Width - _radius;
                        }*/


                    if (rotation < -MathHelper.PiOver2 || rotation >= MathHelper.PiOver2)
                    {
                        _position.X = balls[0].Position.X - (float)(Math.Tan(rotation) * (balls[0].Position.Y - _playableArea.Y - _radius));
                    }
                    else
                    {
                        _position.X = balls[0].Position.X - (float)(Math.Tan(rotation) * (balls[0].Position.Y - _playableArea.Y - _playableArea.Height + _radius));
                    }

                    if (rotation < 0)
                    {
                        _position.Y = balls[0].Position.Y + (float)(Math.Tan(MathHelper.PiOver2 + rotation) * (balls[0].Position.X - _playableArea.X - _radius));
                    }
                    else
                    {
                        _position.Y = balls[0].Position.Y + (float)(Math.Tan(MathHelper.PiOver2 + rotation) * (balls[0].Position.X - _playableArea.X - _playableArea.Width + _radius));
                    }

                    if (_position.X < _playableArea.X + _radius)
                        _position.X = _playableArea.X + _radius;
                    else if (_position.X > _playableArea.X + _playableArea.Width - _radius)
                        _position.X = _playableArea.X + _playableArea.Width - _radius;

                    if (_position.Y < _playableArea.Y + _radius)
                        _position.Y = _playableArea.Y + _radius;
                    else if (_position.Y > _playableArea.Y + _playableArea.Height - _radius)
                        _position.Y = _playableArea.Y + _playableArea.Height - _radius;

                    /*if (rotation < 0)
                    {
                        _position.Y = balls[0].Position.Y - (float)(Math.Tan(rotation) * (balls[0].Position.X - _playableArea.X - _radius));
                    }
                    else
                    {
                        _position.Y = balls[0].Position.Y - (float)(Math.Tan(rotation) * (balls[0].Position.X - _playableArea.X - _playableArea.Width + _radius));
                    }*/
                }
            }
            else
            {
                _position.X = -_radius;
                _position.Y = -_radius;
            }
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            SpriteBatch spriteBatch = ((Pool)this.Game).spriteBatch;
            spriteBatch.Draw(_texture, _position - _radiusVector, _colour);

            base.Draw(gameTime);
        }
    }
}
