﻿using BombAttackGame.Enums;
using BombAttackGame.Events;
using BombAttackGame.Global;
using BombAttackGame.HUD;
using BombAttackGame.Interfaces;
using BombAttackGame.Map;
using BombAttackGame.Models;
using BombAttackGame.Models.HoldableObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;

namespace BombAttackGame {
  public class Game1 : Game
    {
        private readonly List<IGameObject> _gameObjects;
        private readonly List<IGameSprite> _sprites;
        private readonly List<IHoldableObject> _holdableObjects;
        private readonly EventProcessor _eventProcessor;
        private readonly int[] _mapSize = new int[2];
        private readonly GameTime _gameTime;
        private Sheriff _sheriff;
        private MapManager _mapManager;
        private Player _player;
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Vector2 _mousePosition;
        private Color _mainColor;
        private int _teamMateAmount;
        private int _enemyAmount;
        private readonly List<Rectangle> _mapCollision;

        public Game1()
        {
            base.Content.RootDirectory = "Content";
            base.IsMouseVisible = true;
            _mapSize[0] = 1000;
            _mapSize[1] = 1000;
            _graphics = new GraphicsDeviceManager(this);
            _gameObjects = new List<IGameObject>();
            _sprites = new List<IGameSprite>();
            _mapCollision = new List<Rectangle>();
            _gameTime = new GameTime();
            _holdableObjects = new List<IHoldableObject>();
            _eventProcessor = new EventProcessor(_gameObjects, _mapCollision, _gameTime, _sprites);
        }

        protected override void Initialize()
        {

            _mainColor = Color.Tomato;

            base.Window.AllowUserResizing = false;

            _graphics.PreferredBackBufferWidth = _mapSize[0];
            _graphics.PreferredBackBufferHeight = _mapSize[1];
            _graphics.ApplyChanges();

            _enemyAmount = 5;
            _teamMateAmount = 4;

            base.Initialize();

        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            ContentContainer.Initialize(base.Content);

            _mapManager = new MapManager(_spriteBatch);
            _mapCollision.AddRange(_mapManager.Mirage.Rectangle);

            _sheriff = new Sheriff();

            _holdableObjects.Add(_sheriff);

            _gameObjects.Add(GameObject.AddMainSpeed(_mapSize, Team.None, _mapManager));

            _player = GameObject.AddPlayer(Team.TeamMate, _mapSize, _mapManager);
            _gameObjects.Add(_player);
            _player.IsHuman = true;
            _player.Color = Color.Tomato;

            _gameObjects.AddRange(GameObject.AddPlayers(Team.TeamMate, _teamMateAmount, _mapSize, _mapManager));
            _gameObjects.AddRange(GameObject.AddPlayers(Team.Enemy, _enemyAmount, _mapSize, _mapManager));

            foreach(var player in _gameObjects.OfType<Player>())
            {
                player.GiveSheriff(_sheriff);
            }
        }

        protected override void Update(GameTime gameTime)
        {

            _gameTime.TotalGameTime = gameTime.TotalGameTime;
            _gameTime.ElapsedGameTime = gameTime.ElapsedGameTime;
            _gameTime.IsRunningSlowly = gameTime.IsRunningSlowly;

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape)) Exit();

            _mapSize[0] = Window.ClientBounds.Width;
            _mapSize[1] = Window.ClientBounds.Height;

            //_gameObjects.AddRange(_eventProcessor.GameObjects.OfType<Bullet>().Except(_gameObjects.OfType<Bullet>()));

            var kstate = Keyboard.GetState();
            var mstate = Mouse.GetState();

            _mousePosition = mstate.Position.ToVector2();

            foreach (IGameObject GameObject in _gameObjects.OfType<Player>().ToList()) { GameObject.Tick(gameTime, _gameObjects, _mapManager.Mirage.Rectangle); }
            if (kstate.IsKeyDown(Keys.A)) { _player.PlayerMove(Direction.Left); }
            if (kstate.IsKeyDown(Keys.S)) { _player.PlayerMove(Direction.Down); }
            if (kstate.IsKeyDown(Keys.D)) { _player.PlayerMove(Direction.Right); }
            if (kstate.IsKeyDown(Keys.W)) { _player.PlayerMove(Direction.Up); }
            if (kstate.IsKeyDown(Keys.A) && kstate.IsKeyDown(Keys.W)) { _player.PlayerMove(Direction.UpLeft); }
            if (kstate.IsKeyDown(Keys.A) && kstate.IsKeyDown(Keys.S)) { _player.PlayerMove(Direction.DownLeft); }
            if (kstate.IsKeyDown(Keys.D) && kstate.IsKeyDown(Keys.W)) { _player.PlayerMove(Direction.UpRight); }
            if (kstate.IsKeyDown(Keys.D) && kstate.IsKeyDown(Keys.S)) { _player.PlayerMove(Direction.DownRight); }

            if (mstate.LeftButton == ButtonState.Pressed) { _player.UseHoldableItem(_mousePosition); }

            _eventProcessor.ProcessEvents();

            foreach (IGameObject gameObject in _gameObjects.OfType<Bullet>().ToList())
            {
                gameObject.Tick(gameTime, _gameObjects, _mapManager.Mirage.Rectangle);
            }

            foreach (IGameSprite GameSprite in _sprites.ToList()) { GameSprite.Tick(gameTime, _sprites); }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_mainColor);

            _spriteBatch.Begin();
            foreach (var gameObject in _gameObjects) { if (!gameObject.IsDead && _player.VisibleObjects.Contains(gameObject)) { _spriteBatch.Draw(gameObject.Texture, gameObject.Location, gameObject.Color); } }
            foreach (var gameSprite in _sprites) { _spriteBatch.DrawString(gameSprite.Font, gameSprite.Text, gameSprite.Location, gameSprite.Color); }
            for (int i = 0; i < _mapManager.Mirage.WallVector.Count; i++)
            { _spriteBatch.Draw(MapManager.Wall, _mapManager.Mirage.WallVector[i], Color.Red); }

            _spriteBatch.DrawString(ContentContainer.HpFont, _player.Health.ToString(), HudVector.HpVector(_mapSize), Color.Green);

            _spriteBatch.Draw(ContentContainer.SheriffTexture, HudVector.GunVector(_mapSize), Color.FloralWhite);
            _spriteBatch.End();

            base.Draw(gameTime);
        }

    }
}