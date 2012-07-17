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
    public class Ball : DrawableGameComponent
    {
        // Is the ball potted?
        public bool _potted;

        public int _pocketCollided;

        Color _colour;

        // Pockets
        public static List<Rectangle> _pocketRectangles;
        public static List<BoundingSphere> _pocketSpheres;
        public static List<Vector2> _reversedPocketSpheres;

        Texture2D _texture;
        Rectangle _playableArea;

        // Data for properties
        Vector2 _position;
        int _radius;
        BoundingSphere _boundingSphere;
        Vector2 _velocity;

        public Ball(Game game, Texture2D texture, Vector2 position, string colour, Rectangle playableArea)
            : base(game)
        {
            _potted = false;
            _texture = texture;
            _playableArea = playableArea;

            _position = position;
            _radius = (int)Math.Floor(texture.Bounds.Width / 2.0);
            _boundingSphere = new BoundingSphere(new Vector3(_position, 0), _radius);
            _velocity = new Vector2(0, 0);

            switch (colour)
            {
                case "red":
                    _colour = Color.Red;
                    break;
                case "orange":
                    _colour = Color.Orange;
                    break;
                case "black":
                    _colour = Color.Black;
                    break;
                default:
                    _colour = Color.White;
                    break;
            }

            // Set up pocket rectangles
            if (_pocketRectangles == null)
            {
                _pocketRectangles = new List<Rectangle>();
                _pocketRectangles.Add(new Rectangle(0, 0, 131, 131));
                _pocketRectangles.Add(new Rectangle(1149, 0, 131, 131));
                _pocketRectangles.Add(new Rectangle(0, 589, 131, 131));
                _pocketRectangles.Add(new Rectangle(1149, 589, 131, 131));
                _pocketRectangles.Add(new Rectangle(599, 0, 80, 720));
            }

            if (_pocketSpheres == null)
            {
                _pocketSpheres = new List<BoundingSphere>();
                _pocketSpheres.Add(new BoundingSphere(new Vector3(73, 73, 0), 40));
                _pocketSpheres.Add(new BoundingSphere(new Vector3(640, 40, 0), 40));
                _pocketSpheres.Add(new BoundingSphere(new Vector3(1207, 71, 0), 40));
                _pocketSpheres.Add(new BoundingSphere(new Vector3(72, 653, 0), 40));
                _pocketSpheres.Add(new BoundingSphere(new Vector3(640, 684, 0), 40));
                _pocketSpheres.Add(new BoundingSphere(new Vector3(1207, 654, 0), 40));
                _reversedPocketSpheres = new List<Vector2>();
                _reversedPocketSpheres.Add(new Vector2(1150, 600));
                _reversedPocketSpheres.Add(new Vector2(640, 624));
                _reversedPocketSpheres.Add(new Vector2(123, 610));
                _reversedPocketSpheres.Add(new Vector2(1155, 120));
                _reversedPocketSpheres.Add(new Vector2(640, 100));
                _reversedPocketSpheres.Add(new Vector2(119, 116));
            }
        }

        public override void Update(GameTime gameTime)
        {
            // Check for collision with X bounds, move the ball back to bounds, and reverse Vhorz
            bool inPocketArea = false;
            foreach (Rectangle rect in _pocketRectangles)
            {
                // If rectangle fully contains ball
                if (_position.X > rect.X + _radius && _position.X < rect.X + rect.Width - _radius
                    && _position.Y > rect.Y + _radius && _position.Y < rect.Y + rect.Height - _radius)
                {
                    inPocketArea = true;
                }
            }

            if (!inPocketArea)
            {
                if (X < _playableArea.X + _radius)
                {
                    X = _playableArea.X + _radius;
                    _velocity.X *= -1;
                    Velocity *= 0.95f;
                    ((Pool)this.Game)._ballStrike.Play(Velocity.LengthSquared() / 5000000, 0.0f, 0.0f);
                }
                else if (X > _playableArea.X + _playableArea.Width - _radius)
                {
                    X = _playableArea.X + _playableArea.Width - _radius;
                    _velocity.X *= -1;
                    Velocity *= 0.95f;
                    ((Pool)this.Game)._ballStrike.Play(Velocity.LengthSquared() / 5000000, 0.0f, 0.0f);
                }

                // Check for collision with Y bounds
                if (Y < _playableArea.Y + _radius)
                {
                    double test = Velocity.LengthSquared();
                    Y = _playableArea.Y + _radius;
                    _velocity.Y *= -1;
                    Velocity *= 0.95f;
                    ((Pool)this.Game)._ballStrike.Play(Velocity.LengthSquared() / 5000000, 0.0f, 0.0f);
                }
                else if (Y > _playableArea.Y + _playableArea.Height - _radius)
                {
                    Y = _playableArea.Y + _playableArea.Height - _radius;
                    _velocity.Y *= -1;
                    Velocity *= 0.95f;
                    ((Pool)this.Game)._ballStrike.Play(Velocity.LengthSquared() / 5000000, 0.0f, 0.0f);
                }
            }

            Position += _velocity * gameTime.ElapsedGameTime.Milliseconds / 1000;

            float friction;
            if (gameTime.ElapsedGameTime.Milliseconds != 0)
                friction = 1 / (1.0f + (0.001f * gameTime.ElapsedGameTime.Milliseconds));
            else
                friction = 0.99f;

            Velocity *= friction;
            //Velocity *= (float)(0.85 + 2 *(generator.NextDouble() / 5.0));

            if (_velocity.LengthSquared() < 100)
                _velocity *= 0;

            for (int i = 0; i < _pocketSpheres.Count; i++)
            {
                // If the ball's in a pocket
                if (BoundingSphere.Intersects(_pocketSpheres[i]))
                {
                    // Set it to potted, causing it to be pruned by the GameUpdate()
                    _pocketCollided = i;
                    _potted = true;
                }
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            if (Visible)
                ((Pool)this.Game).spriteBatch.Draw(_texture, new Vector2(Position.X - _radius, Position.Y - _radius), _colour);

            base.Draw(gameTime);
        }

        public Vector2 Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
                _boundingSphere.Center.X = value.X;
                _boundingSphere.Center.Y = value.Y;
            }
        }

        public double X
        {
            get
            {
                return _position.X;
            }
            set
            {
                _position.X = (float)value;
                _boundingSphere.Center.X = (float)value;
            }
        }

        public double Y
        {
            get
            {
                return _position.Y;
            }
            set
            {
                _position.Y = (float)value;
                _boundingSphere.Center.Y = (float)value;
            }
        }

        public BoundingSphere BoundingSphere
        {
            get
            {
                return _boundingSphere;
            }
            set
            {
                // Created automatically in constructor, updated by Position
            }
        }

        public Vector2 Velocity
        {
            get
            {
                return _velocity;
            }
            set
            {
                _velocity = value;
            }
        }

        public Color Colour
        {
            get
            {
                return _colour;
            }
            set
            {
                _colour = value;
            }
        }

        public void SetVelocity(double speed, double direction)
        {
            _velocity.X = (float)(Math.Sin(direction) * speed);
            _velocity.Y = (float)(Math.Cos(direction) * speed);
        }
    }
}
