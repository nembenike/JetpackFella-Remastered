using JetpackFella_Remastered;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace JetpackFella_Remastered
{
    public class Game
    {
        private readonly Canvas _canvas;
        private readonly int SCREEN_WIDTH;
        private readonly int SCREEN_HEIGHT;

        private readonly List<Asteroid> _asteroids = new();
        private readonly List<Rectangle> _asteroidVisuals = new();
        private readonly Random _rnd = new();

        // visuals
        private readonly Rectangle _playerVisual;
        private readonly Rectangle _fuelBar;
        private readonly Rectangle _pickupVisual;
        private readonly Rectangle _upgradeVisual;
        private readonly TextBlock _scoreText;
        private readonly TextBlock _fpsText;

        // game objects
        private readonly Player _player = new();
        private readonly Pickup _fuelPickup = new();
        private readonly Pickup _upgrade = new();
        private readonly FuelSystem _fuel = new() { Current = 50, Maximum = 50 };
        private readonly GameResources _resources = new();

        // state
        private GameState _state = GameState.Menu;
        private int _numAsteroids = 0;

        // store last score for Game Over screen
        private int _lastScore = 0;

        // constants
        private const double BASE_FUEL = 100.0;
        private const double FUEL_PICKUP_VALUE = 15;
        private const int UPGRADE_FUEL_BONUS = 10;
        private const int MAX_ASTEROIDS = 10;

        // fps tracking
        private readonly Stopwatch _fpsStopwatch = new();
        private int _frameCount = 0;
        private double _currentFps = 0;

        public Game(Canvas canvas, int screenWidth, int screenHeight)
        {
            _canvas = canvas;
            SCREEN_WIDTH = Math.Max(100, screenWidth);
            SCREEN_HEIGHT = Math.Max(100, screenHeight);

            // create visuals
            _playerVisual = new Rectangle { Width = 20, Height = 20, Fill = Brushes.Cyan };
            _fuelBar = new Rectangle { Width = 20, Height = BASE_FUEL, Fill = Brushes.Orange };
            _pickupVisual = new Rectangle { Width = 20, Height = 20, Fill = Brushes.Yellow };
            _upgradeVisual = new Rectangle { Width = 20, Height = 20, Fill = Brushes.Green };
            _scoreText = new TextBlock { Foreground = Brushes.White, FontSize = 16 };
            _fpsText = new TextBlock { Foreground = Brushes.White, FontSize = 14 };

            _canvas.Children.Add(_playerVisual);
            _canvas.Children.Add(_fuelBar);
            _canvas.Children.Add(_pickupVisual);
            _canvas.Children.Add(_upgradeVisual);
            _canvas.Children.Add(_scoreText);
            _canvas.Children.Add(_fpsText);

            // initialize asteroid visuals
            for (int i = 0; i < MAX_ASTEROIDS; i++)
            {
                var rect = new Rectangle { Fill = Brushes.Gray, Tag = "AST" };
                _asteroidVisuals.Add(rect);
                _canvas.Children.Add(rect);

                _asteroids.Add(new Asteroid { Rect = new Rect(-100, -100, 0, 0), Speed = new Vector(0, 0), Rotation = 0 });
            }

            _fpsStopwatch.Start();
            CompositionTarget.Rendering += GameLoop;

            InitGame();
        }

        private void InitGame()
        {
            // reset player
            _player.Position = new Point(SCREEN_WIDTH / 2.0, SCREEN_HEIGHT / 2.0);
            _player.Gravity = 0;
            _player.IsJetpacking = false;
            _player.Size = 20;

            // reset fuel
            _fuel.Current = BASE_FUEL;
            _fuel.Maximum = (int)BASE_FUEL;

            // reset pickups
            _fuelPickup.IsSpawned = true;
            _fuelPickup.Rect = new Rect(_rnd.Next(0, Math.Max(1, SCREEN_WIDTH - 20)),
                                        _rnd.Next(0, Math.Max(1, SCREEN_HEIGHT - 20)), 20, 20);

            _upgrade.IsSpawned = false;
            _upgrade.Rect = new Rect(-100, -100, 20, 20);

            // reset score only for the new game
            _resources.Score = 0;

            // reset asteroids
            _numAsteroids = 0;
            for (int i = 0; i < _asteroids.Count; i++)
            {
                _asteroids[i].Rect = new Rect(-100, -100, 0, 0);
                _asteroidVisuals[i].Visibility = Visibility.Collapsed;
            }

            _state = GameState.Menu;

            UpdateVisualsImmediate();
        }

        public void Start()
        {
            _state = GameState.Menu;
        }

        public void KeyDown(Key key)
        {
            if (key == Key.Space)
            {
                if (_state == GameState.Menu)
                {
                    _state = GameState.Playing;
                }
                else if (_state == GameState.GameOver)
                {
                    InitGame();
                    _state = GameState.Playing;
                }

                _player.IsJetpacking = true;
            }
        }

        public void KeyUp(Key key)
        {
            if (key == Key.Space)
            {
                _player.IsJetpacking = false;
            }
        }

        private void GameLoop(object sender, EventArgs e)
        {
            double dt = 1.0 / 60.0;

            if (_state == GameState.Playing)
                Update(dt);

            Draw();
            UpdateVisualsImmediate();

            // fps tracking
            _frameCount++;
            if (_fpsStopwatch.ElapsedMilliseconds >= 1000)
            {
                _currentFps = _frameCount * 1000.0 / _fpsStopwatch.ElapsedMilliseconds;
                _frameCount = 0;
                _fpsStopwatch.Restart();
                _fpsText.Text = $"FPS: {Math.Round(_currentFps)}";
            }
        }

        private void Update(double dt)
        {
            UpdatePlayer();
            UpdateAsteroids();
            UpdatePickups();

            // check death
            if (_player.Position.Y > SCREEN_HEIGHT)
            {
                _lastScore = _resources.Score; 
                _state = GameState.GameOver;
                return; // do not reset yet
            }
        }

        private void UpdatePlayer()
        {
            double moveSpeed = (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) ? 4.0 : 2.0;

            if (Keyboard.IsKeyDown(Key.D) || Keyboard.IsKeyDown(Key.Right))
                _player.Position = new Point(_player.Position.X + moveSpeed, _player.Position.Y);
            if (Keyboard.IsKeyDown(Key.A) || Keyboard.IsKeyDown(Key.Left))
                _player.Position = new Point(_player.Position.X - moveSpeed, _player.Position.Y);

            if ((_player.IsJetpacking || Keyboard.IsKeyDown(Key.Space)) && _fuel.Current > 0)
            {
                _player.Gravity = Keyboard.IsKeyDown(Key.S) ? -4.0 : 4.0;
                _fuel.Current = Math.Max(0, _fuel.Current - 0.5);
            }
            else
            {
                _player.Gravity -= 0.17;
            }

            _player.Position = new Point(_player.Position.X, _player.Position.Y - _player.Gravity);

            if (_player.Position.X > SCREEN_WIDTH) _player.Position = new Point(0, _player.Position.Y);
            else if (_player.Position.X < 0) _player.Position = new Point(SCREEN_WIDTH, _player.Position.Y);

            if (_player.Position.Y < 0) _player.Position = new Point(_player.Position.X, 0);
        }

        private void UpdateAsteroids()
        {
            if (_numAsteroids < MAX_ASTEROIDS && _rnd.Next(0, 100) < 10 && _resources.Score >= 3)
            {
                SpawnAsteroid(_numAsteroids);
                _numAsteroids++;
            }

            for (int i = 0; i < _numAsteroids; i++)
            {
                var a = _asteroids[i];
                a.Rect = new Rect(a.Rect.X + a.Speed.X, a.Rect.Y + a.Speed.Y, a.Rect.Width, a.Rect.Height);

                if (a.Rect.Width > 0 && a.Rect.Height > 0 && a.Rect.IntersectsWith(_player.Rect))
                {
                    double damage = a.Rect.Width / 3.0;
                    _fuel.Current = Math.Max(0, _fuel.Current - damage);
                    SpawnAsteroid(i);
                }

                if (a.Rect.Y > SCREEN_HEIGHT)
                    SpawnAsteroid(i);
            }
        }

        private void SpawnAsteroid(int index)
        {
            double w = _rnd.Next(15, Math.Max(16, _resources.Score * 3));
            double x = _rnd.Next(0, Math.Max(1, SCREEN_WIDTH - 50));
            _asteroids[index].Rect = new Rect(x, 0, w, w);
            _asteroids[index].Speed = new Vector(0, 80.0 / w);
            _asteroids[index].Rotation = _rnd.Next(0, 360);
        }

        private void UpdatePickups()
        {
            if (_player.Rect.IntersectsWith(_fuelPickup.Rect) && _fuelPickup.IsSpawned)
            {
                _fuel.Current = Math.Min(_fuel.Current + FUEL_PICKUP_VALUE, _fuel.Maximum);
                _fuelPickup.IsSpawned = false;
                _resources.Score++;
            }

            if (_upgrade.IsSpawned && _player.Rect.IntersectsWith(_upgrade.Rect))
            {
                _fuel.Maximum += UPGRADE_FUEL_BONUS;
                _fuel.Current += 5;
                _resources.Score++;
                _upgrade.IsSpawned = false;
            }

            if (!_fuelPickup.IsSpawned)
            {
                _fuelPickup.Rect = new Rect(GetRandom(50, SCREEN_WIDTH - 20), GetRandom(20, SCREEN_HEIGHT - 50), 20, 20);
                _fuelPickup.IsSpawned = true;
            }

            if (_resources.Score > 0 && _resources.Score % 10 == 0 && !_upgrade.IsSpawned)
            {
                _upgrade.Rect = new Rect(GetRandom(0, SCREEN_WIDTH - 20), GetRandom(0, SCREEN_HEIGHT - 20), 20, 20);
                _upgrade.IsSpawned = true;
            }

            double barHeight = Math.Max(0, _fuel.Current);
            _fuelBar.Height = barHeight;
            Canvas.SetTop(_fuelBar, 15 + (BASE_FUEL - barHeight));
        }

        private void Draw()
        {
            for (int i = 0; i < _numAsteroids; i++)
            {
                var a = _asteroids[i];
                var rect = _asteroidVisuals[i];
                rect.Width = a.Rect.Width;
                rect.Height = a.Rect.Height;

                Canvas.SetLeft(rect, a.Rect.X);
                Canvas.SetTop(rect, a.Rect.Y);

                // apply rotation
                rect.RenderTransform = new RotateTransform(a.Rotation, a.Rect.Width / 2, a.Rect.Height / 2);

                rect.Visibility = a.Rect.Width > 0 && a.Rect.Height > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_state == GameState.Menu)
            {
                _scoreText.Text = "Press Space to Play";
                Canvas.SetLeft(_scoreText, (SCREEN_WIDTH - 200) / 2);
                Canvas.SetTop(_scoreText, (SCREEN_HEIGHT - 20) / 2);
            }
            else if (_state == GameState.GameOver)
            {
                _scoreText.Text = $"Game Over - Score: {_lastScore} (Space to restart)";
                Canvas.SetLeft(_scoreText, 100);
                Canvas.SetTop(_scoreText, SCREEN_HEIGHT / 2);
            }
        }

        private void UpdateVisualsImmediate()
        {
            Canvas.SetLeft(_playerVisual, _player.Position.X);
            Canvas.SetTop(_playerVisual, _player.Position.Y);

            Canvas.SetLeft(_pickupVisual, _fuelPickup.Rect.X);
            Canvas.SetTop(_pickupVisual, _fuelPickup.Rect.Y);
            _pickupVisual.Visibility = _fuelPickup.IsSpawned ? Visibility.Visible : Visibility.Collapsed;

            Canvas.SetLeft(_upgradeVisual, _upgrade.Rect.X);
            Canvas.SetTop(_upgradeVisual, _upgrade.Rect.Y);
            _upgradeVisual.Visibility = _upgrade.IsSpawned ? Visibility.Visible : Visibility.Collapsed;

            Canvas.SetRight(_scoreText, 10);
            Canvas.SetTop(_scoreText, 10);
            if (_state == GameState.Playing)
                _scoreText.Text = $"Score: {_resources.Score}";

            Canvas.SetRight(_fpsText, 10);
            Canvas.SetTop(_fpsText, 30);
        }

        private int GetRandom(int a, int b) => _rnd.Next(a, b + 1);
    }
}
