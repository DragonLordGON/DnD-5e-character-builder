using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace DndCharacterBuilder.Controls
{
    public partial class DiceControl : UserControl
    {
        private Vector _velocity;
        private double _rotationSpeed;
        private DispatcherTimer _timer;
        private Random _rng = new Random();
        private int _finalValue;
        private int _sides;
        private Avalonia.Media.RotateTransform DiceRotation;

        public DiceControl()
        {
            InitializeComponent();
            DiceRotation = new Avalonia.Media.RotateTransform();
        }

        /// <summary>
        /// Starts the dice roll animation.
        /// </summary>
        /// <param name="finalValue">The number the dice should land on.</param>
        /// <param name="sides">Total sides of the dice (e.g., 20 for d20).</param>
        /// <param name="bounds">The boundary rect (usually the parent Canvas bounds).</param>
        public void Roll(int finalValue, int sides, Rect bounds)
        {
            _finalValue = finalValue;
            _sides = sides;
            
            // Random initial velocity
            double speedX = (_rng.NextDouble() * 20 - 10);
            double speedY = (_rng.NextDouble() * 20 - 10);
            _velocity = new Vector(speedX, speedY);
            
            // Random initial rotation
            _rotationSpeed = _rng.Next(15, 40);

            if (_timer != null) _timer.Stop();
            
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60fps
            _timer.Tick += (s, e) => UpdatePosition(bounds);
            _timer.Start();
        }

        private void UpdatePosition(Rect bounds)
        {
            double currentX = Canvas.GetLeft(this);
            double currentY = Canvas.GetTop(this);
            
            // Handle NaN or uninitialized positions
            if (double.IsNaN(currentX)) currentX = 0;
            if (double.IsNaN(currentY)) currentY = 0;

            double nextX = currentX + _velocity.X;
            double nextY = currentY + _velocity.Y;

            // Collision detection with bounds
            if (nextX <= 0 || nextX >= bounds.Width - Width)
            {
                _velocity = new Vector(-_velocity.X, _velocity.Y);
                nextX = Math.Clamp(nextX, 0, bounds.Width - Width);
            }
            
            if (nextY <= 0 || nextY >= bounds.Height - Height)
            {
                _velocity = new Vector(_velocity.X, -_velocity.Y);
                nextY = Math.Clamp(nextY, 0, bounds.Height - Height);
            }

            Canvas.SetLeft(this, nextX);
            Canvas.SetTop(this, nextY);

            // Update rotation
            DiceRotation.Angle += _rotationSpeed;

            // While rolling fast, show random numbers
            if (_velocity.Length > 2)
            {
                DiceText.Text = _rng.Next(1, _sides + 1).ToString();
            }

            // Apply friction/air resistance
            _velocity *= 0.97;
            _rotationSpeed *= 0.97;

            // Check if stopped
            if (_velocity.Length < 0.3)
            {
                _timer.Stop();
                FinalizeRoll();
            }
        }

        private void FinalizeRoll()
        {
            _velocity = new Vector(0, 0);
            _rotationSpeed = 0;
            DiceRotation.Angle = 0;
            DiceText.Text = _finalValue.ToString();
            
            // Visual feedback for landing (optional)
            DiceBody.BorderBrush = Avalonia.Media.Brushes.Gold;
        }
    }
}
