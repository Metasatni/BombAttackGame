﻿using BombAttackGame.Abstracts;
using BombAttackGame.Draw;
using BombAttackGame.Enums;
using BombAttackGame.Global;
using BombAttackGame.Interfaces;
using BombAttackGame.Map;
using BombAttackGame.Models;
using BombAttackGame.Models.HoldableObjects;
using BombAttackGame.Models.HoldableObjects.ThrowableObjects;
using BombAttackGame.Vector;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BombAttackGame.Events
{
    internal class EventProcessor
    {
        private readonly List<IGameObject> _gameObjects;
        private readonly List<IGameSprite> _sprites;
        private readonly List<Rectangle> _mapCollisions;
        private readonly List<IHoldableObject> _holdableObjects;
        private readonly List<IOnGroundItem> _onGroundItems;
        private readonly GameManager _gameManager;
        private readonly MapManager _mapManager;
        private List<Animation> _animations;
        private readonly GameTime _gameTime;
        private Bomb _bomb;
        public bool EndGameBool;

        public EventProcessor(List<IGameObject> gameObjects, List<Rectangle> mapCollisions, GameTime gameTime,
            List<IGameSprite> sprites, List<IHoldableObject> holdableObjects, List<Animation> animations, GameManager gameManager,
            MapManager mapManager, List<IOnGroundItem> onGroundItems)
        {
            _gameObjects = gameObjects;
            _mapCollisions = mapCollisions;
            _gameTime = gameTime;
            _sprites = sprites;
            _holdableObjects = holdableObjects;
            _animations = animations;
            _gameManager = gameManager;
            _mapManager = mapManager;
            _onGroundItems = onGroundItems;
        }

        public void ProcessEvents()
        {
            foreach (var gameObject in _gameObjects.ToList())
            {
                if (gameObject.Event is null) continue;
                while (gameObject.Event.TryDequeue(out Enums.Events e))
                {
                    ProcessEvent(gameObject, e);
                }
            }
            foreach (var holdableObject in _holdableObjects.ToList())
            {
                if (holdableObject.Event is null) continue;
                while (holdableObject.Event.TryDequeue(out Enums.Events e))
                {
                    ProcessEvent(holdableObject, e);
                }
            }
            ProcessEvent(_gameManager, _gameManager.Event);
            if (_bomb != null)
            {
                if (_gameTime.TotalGameTime.TotalMilliseconds >= _bomb.BoomTime && _bomb.BoomTime != 0 && !_bomb.Exploded)
                {
                    _bomb.Explode();
                }
                ProcessEvent(_bomb, _bomb.Event);

            }
        }

        private void ProcessEvent(Bomb bomb, Enums.Events e)
        {
            switch (e)
            {
                case Enums.Events.Explode:
                    Explode(bomb);
                    break;
            }
        }

        private void ProcessEvent(IGameObject gameObject, Enums.Events e)
        {
            switch (e)
            {
                case Enums.Events.Move:
                    Move(gameObject);
                    break;
                case Enums.Events.Dead:
                    Dead(gameObject);
                    break;
                case Enums.Events.TryShoot:
                    TryShoot(gameObject);
                    break;
                case Enums.Events.PlantBomb:
                    PlantBomb(gameObject);
                    break;
                case Enums.Events.Throw:
                    ThrowItem(gameObject);
                    break;
                case Enums.Events.ObjectHitted:
                    ObjectHitted(gameObject);
                    break;
                case Enums.Events.Shoot:
                    break;
                case Enums.Events.Delete:
                    Delete(gameObject);
                    break;
                case Enums.Events.Explode:
                    Explode(gameObject);
                    break;
                case Enums.Events.DropBomb:
                    DropBomb(gameObject);
                    break;
            }
        }

        private void DropBomb(IGameObject gameObject)
        {
            Player p = (Player)gameObject;
            Bomb b = Drop.DropBomb(p);
            if (b.Location != new Vector2(0, 0))
            {
                b.Position = p.Position;
                 _onGroundItems.Add(b);
            }
        }

        private void PlantBomb(IGameObject gameObject)
        {
            Player player = (Player)gameObject;
            var bomb = (Bomb)player.Inventory.Slot4;

            List<Point> aPoints = new List<Point>(MapManager.GetPointsFromChar('a'));
            List<Point> bPoints = new List<Point>(MapManager.GetPointsFromChar('b'));
            if (aPoints.Contains(player.Position) || bPoints.Contains(player.Position))
            {
                bomb.Plant(player.Location, _gameTime.TotalGameTime.TotalMilliseconds);
                bomb.Position = player.Position;
                _onGroundItems.Add(bomb);
                _bomb = bomb;
                player.RemoveFromInventory(bomb);
            }
        }

        private void Dead(IGameObject gameObject)
        {
            _gameObjects.Remove(gameObject);
        }

        private void ProcessEvent(GameManager gameManager, Enums.Events e)
        {
            switch (e)
            {
                case Enums.Events.StartRound:
                    StartRound();
                    break;
                case Enums.Events.EndGame:
                    EndGame();
                    break;
                case Enums.Events.TimeLapse:
                    TimeLapse();
                    break;
            }
        }
        private void TimeLapse()
        {
            foreach (var player in _gameObjects.OfType<Player>())
            {
                player.Speed = 10;
                if (player.Inventory.Slot1 is Sheriff sheriff) sheriff.Latency = 20;
            }
            foreach (var bullet in _gameObjects.OfType<Bullet>())
            {
                bullet.Speed = 10;
            }
            foreach (var nade in _gameObjects.OfType<Grenade>())
            {
                nade.Speed = 100;
            }

            _gameManager.ResetEvent();
        }
        private void StartRound()
        {
            if (GameManager.StartRoundTime == 0) { GameManager.StartRoundTime = _gameTime.TotalGameTime.TotalMilliseconds + GameManager.StartRoundLatency; }
            if (_gameTime.TotalGameTime.TotalMilliseconds <= GameManager.StartRoundTime) return;
            Player player = new Player(Team.TeamMate);
            this._holdableObjects.Clear();
            this._gameObjects.Clear();
            this._onGroundItems.Clear();
            this._bomb = null;
            player = GameObject.AddPlayer(Team.TeamMate, _mapManager);
            _gameObjects.Add(player);
            player.IsHuman = true;
            player.Color = Color.Tomato;
            _gameObjects.AddRange(GameObject.AddPlayers(Team.TeamMate, _gameManager.TeamMatesCount, _mapManager));
            _gameObjects.AddRange(GameObject.AddPlayers(Team.Enemy, _gameManager.EnemyCount, _mapManager));

            foreach (var p in _gameObjects.OfType<Player>())
            {
                var gun = new Sheriff();
                p.Inventory.InventoryItems.Add(gun);
                p.Inventory.Equip(gun);
                player.Inventory.Equip(new FlashGrenade(player));
                player.Inventory.Equip(new HandGrenade(player));
                p.Inventory.Select(1);
                _holdableObjects.Add(gun);
            }

            List<Player> tplayers = new List<Player>();
            tplayers.AddRange(this._gameObjects.OfType<Player>().Where(x => x.Team == Team.Enemy));
            int r = new Random().Next(0, tplayers.Count);
            tplayers.ElementAt(r).Inventory.Equip(new Bomb());

            _gameManager.SetTime(_gameTime);
            _gameManager.ResetEvent();
            _gameManager.Reset();
        }
        private void EndGame()
        {
            this.EndGameBool = true;
        }

        private void ThrowItem(IGameObject gameObject)
        {
            Explosive item = Throw.PlayerThrow((Player)gameObject);
            if (item is HandGrenade)
            {
                HandGrenade handGrenade = (HandGrenade)item;
                handGrenade.StartTime = _gameTime.TotalGameTime.TotalMilliseconds;
                _gameObjects.Add(handGrenade);
            }
            if (item is FlashGrenade)
            {
                FlashGrenade flashGrenade = (FlashGrenade)item;
                flashGrenade.StartTime = _gameTime.TotalGameTime.TotalMilliseconds;
                _gameObjects.Add(flashGrenade);
            }
        }
        private void Explode(Bomb bomb)
        {
            DealDamageAround(bomb);
            _animations.Add(new Animation(AnimationsContainer.BombBoom, bomb.Location, 5));
            bomb.Event = Enums.Events.None;
        }
        private void Explode(IGameObject gameObject)
        {
            switch (gameObject)
            {
                case HandGrenade handGrenade:
                    DealDamageAround(handGrenade);
                    _animations.Add(new Animation(AnimationsContainer.HandGrenadeBoom, handGrenade.Location));
                    break;
                case FlashGrenade flashGrenade:
                    FlashAround(flashGrenade); break;
            }
        }
        private void FlashAround(FlashGrenade flashGrenade)
        {
            foreach (var obj in _gameObjects.OfType<Player>())
            {
                if (obj.VisibleObjects.Any(x => flashGrenade == x))
                {
                    int time = flashGrenade.CalculateTime(obj);
                    if (time > 0)
                    {
                        FlashPlayers.Flash(obj, (int)_gameTime.TotalGameTime.TotalMilliseconds + time);
                    }
                }
            }
        }
        private void DealDamageAround(IOnGroundItem h)
        {
            if (h is Bomb)
            {
                var bomb = (Bomb)h;
                foreach (var obj in _gameObjects.OfType<Player>())
                {
                    int damage = bomb.CalculateDamage(obj);
                    if (damage > 0)
                    {
                        DealDamage.DealDamageToPlayer(obj, damage);
                        CreateDamage(bomb, _gameTime);
                    }
                }
            }
        }
        private void DealDamageAround(IHoldableObject h)
        {
            if (h is HandGrenade)
            {
                var handGrenade = (HandGrenade)h;
                foreach (var obj in _gameObjects.OfType<Player>())
                {
                    int damage = handGrenade.CalculateDamage(obj);
                    if (damage > 0)
                    {
                        DealDamage.DealDamageToPlayer(obj, damage);
                        CreateDamage(handGrenade, obj, _gameTime);
                    }
                }
            }
        }

        private void ProcessEvent(IHoldableObject obj, Enums.Events e)
        {
            switch (e)
            {
                case Enums.Events.Reload:
                    var o = obj as Gun;
                    o.Reload();
                    break;
            }
        }

        private void Move(IGameObject gameObject)
        {
            switch (gameObject)
            {
                case Player player: Move(player); break;
                case Bullet bullet: Move(bullet); break;
                case HandGrenade handgrenade: Move(handgrenade); break;
                case FlashGrenade flashGrenade: Move(flashGrenade); break;
            }
        }

        private void Move(Player player)
        {
            if(player.IsDead) return;
            Vector2 newLocation;
            Rectangle rectangle;
            switch (player.Direction)
            {
                case Direction.Left:
                    newLocation = new Vector2(player.Location.X - (int)player.Speed, player.Location.Y);
                    rectangle = new Rectangle((int)newLocation.X, (int)newLocation.Y, player.Texture.Width, player.Texture.Height);
                    if (InRectangle(rectangle)) return;
                    player.Location = newLocation;
                    player.Direction = Direction.Left;
                    break;
                case Direction.Right:
                    newLocation = new Vector2(player.Location.X + (int)player.Speed, player.Location.Y);
                    rectangle = new Rectangle((int)newLocation.X, (int)newLocation.Y, player.Texture.Width, player.Texture.Height);
                    if (InRectangle(rectangle)) return;
                    player.Location = newLocation;
                    player.Direction = Direction.Right;
                    break;
                case Direction.Up:
                    newLocation = new Vector2(player.Location.X, player.Location.Y - (int)player.Speed);
                    rectangle = new Rectangle((int)newLocation.X, (int)newLocation.Y, player.Texture.Width, player.Texture.Height);
                    if (InRectangle(rectangle)) return;
                    player.Location = newLocation;
                    player.Direction = Direction.Up;
                    break;
                case Direction.Down:
                    newLocation = new Vector2(player.Location.X, player.Location.Y + (int)player.Speed);
                    rectangle = new Rectangle((int)newLocation.X, (int)newLocation.Y, player.Texture.Width, player.Texture.Height);
                    if (InRectangle(rectangle)) return;
                    player.Location = newLocation;
                    player.Direction = Direction.Down;
                    break;
                case Direction.UpLeft:
                    newLocation = new Vector2(player.Location.X - (int)player.Speed, player.Location.Y - (int)player.Speed);
                    rectangle = new Rectangle((int)newLocation.X, (int)newLocation.Y, player.Texture.Width, player.Texture.Height);
                    if (InRectangle(rectangle))
                    {
                        newLocation = new Vector2(player.Location.X, player.Location.Y - (int)player.Speed);
                        rectangle = new Rectangle((int)newLocation.X, (int)newLocation.Y, player.Texture.Width, player.Texture.Height);
                        if (InRectangle(rectangle))
                        {
                            newLocation = new Vector2(player.Location.X - (int)player.Speed, player.Location.Y);
                            rectangle = new Rectangle((int)newLocation.X, (int)newLocation.Y, player.Texture.Width, player.Texture.Height);
                            if (InRectangle(rectangle)) return;
                            player.Location = newLocation;
                            player.Direction = Direction.Left;
                        }
                        player.Location = newLocation;
                        player.Direction = Direction.Up;
                    }
                    player.Location = newLocation;
                    player.Direction = Direction.UpLeft;
                    break;
                case Direction.DownLeft:
                    newLocation = new Vector2(player.Location.X - (int)player.Speed, player.Location.Y + (int)player.Speed);
                    rectangle = new Rectangle((int)newLocation.X, (int)newLocation.Y, player.Texture.Width, player.Texture.Height);
                    if (InRectangle(rectangle))
                    {
                        newLocation = new Vector2(player.Location.X, player.Location.Y + (int)player.Speed);
                        rectangle = new Rectangle((int)newLocation.X, (int)newLocation.Y, player.Texture.Width, player.Texture.Height);
                        if (InRectangle(rectangle))
                        {
                            newLocation = new Vector2(player.Location.X - (int)player.Speed, player.Location.Y);
                            rectangle = new Rectangle((int)newLocation.X, (int)newLocation.Y, player.Texture.Width, player.Texture.Height);
                            if (InRectangle(rectangle)) return;
                            player.Location = newLocation;
                            player.Direction = Direction.Left;
                        }
                        player.Location = newLocation;
                        player.Direction = Direction.Down;
                    }
                    player.Location = newLocation;
                    player.Direction = Direction.DownLeft;
                    break;
                case Direction.DownRight:
                    newLocation = new Vector2(player.Location.X + (int)player.Speed, player.Location.Y + (int)player.Speed);
                    rectangle = new Rectangle((int)newLocation.X, (int)newLocation.Y, player.Texture.Width, player.Texture.Height);
                    if (InRectangle(rectangle))
                    {
                        newLocation = new Vector2(player.Location.X, player.Location.Y + (int)player.Speed);
                        rectangle = new Rectangle((int)newLocation.X, (int)newLocation.Y, player.Texture.Width, player.Texture.Height);
                        if (InRectangle(rectangle))
                        {
                            newLocation = new Vector2(player.Location.X + (int)player.Speed, player.Location.Y);
                            rectangle = new Rectangle((int)newLocation.X, (int)newLocation.Y, player.Texture.Width, player.Texture.Height);
                            if (InRectangle(rectangle)) return;
                            player.Location = newLocation;
                            player.Direction = Direction.Right;
                        }
                        player.Location = newLocation;
                        player.Direction = Direction.Down;
                    }
                    player.Location = newLocation;
                    player.Direction = Direction.DownRight;
                    break;
                case Direction.UpRight:
                    newLocation = new Vector2(player.Location.X + (int)player.Speed, player.Location.Y - (int)player.Speed);
                    rectangle = new Rectangle((int)newLocation.X, (int)newLocation.Y, player.Texture.Width, player.Texture.Height);
                    if (InRectangle(rectangle))
                    {
                        newLocation = new Vector2(player.Location.X, player.Location.Y - (int)player.Speed);
                        rectangle = new Rectangle((int)newLocation.X, (int)newLocation.Y, player.Texture.Width, player.Texture.Height);
                        if (InRectangle(rectangle))
                        {
                            newLocation = new Vector2(player.Location.X + (int)player.Speed, player.Location.Y);
                            rectangle = new Rectangle((int)newLocation.X, (int)newLocation.Y, player.Texture.Width, player.Texture.Height);
                            if (InRectangle(rectangle)) return;
                            player.Location = newLocation;
                            player.Direction = Direction.Right;
                        }
                        player.Location = newLocation;
                        player.Direction = Direction.Up;
                    }
                    player.Location = newLocation;
                    player.Direction = Direction.UpRight;
                    break;
            }
            player.Position = new Point((int)player.Location.X / 20, (int)player.Location.Y / 20);
            foreach (var item in _onGroundItems.ToList())
            {
                if (item.Position == player.Position)
                {
                    if (item is Bomb)
                    {
                        Bomb b = (Bomb)item;
                        if (b.Planted) return;
                        if (player.Team == Team.Enemy)
                        {
                            player.Inventory.Equip(item);
                            item.Position = new Point(0, 0);
                            item.Location = new Vector2(0, 0);
                            _onGroundItems.Remove(item);
                        }
                    }
                    else
                    {
                        player.Inventory.Equip(item);
                        item.Position = new Point(0, 0);
                        item.Location = new Vector2(0, 0);
                        _onGroundItems.Remove(item);
                    }
                }
            }
            player.ChangeTexture();
        }

        private void Move(Bullet bullet)
        {
            float speed = bullet.Speed * (1.0f / bullet.Distance);
            float length = 0;
            bullet.Location = Vector2.Lerp(bullet.Location, bullet.Point, speed);
            length += Vector2.Distance(bullet.Location, bullet.StartLocation);
            bullet.DistanceTravelled += length;
            foreach (var rec in _mapCollisions)
            {
                if (bullet.Rectangle.Intersects(rec)) bullet.Event.Enqueue(Enums.Events.ObjectHitted);
            }
        }
        private void Move(HandGrenade handGrenade)
        {
            float speed = handGrenade.Speed * (1.0f / handGrenade.Distance);
            float length = 0;
            handGrenade.Location = Vector2.Lerp(handGrenade.Location, handGrenade.Point, speed);
            length += Vector2.Distance(handGrenade.Location, handGrenade.StartLocation);
            handGrenade.DistanceTravelled += length;
            foreach (var rec in _mapCollisions)
            {
                if (handGrenade.Rectangle.Intersects(rec))
                    handGrenade.Event.Enqueue(Enums.Events.ObjectHitted);
            }
        }

        private void Move(FlashGrenade flashGrenade)
        {
            float speed = flashGrenade.Speed * (1.0f / flashGrenade.Distance);
            float length = 0;
            flashGrenade.Location = Vector2.Lerp(flashGrenade.Location, flashGrenade.Point, speed);
            length += Vector2.Distance(flashGrenade.Location, flashGrenade.StartLocation);
            flashGrenade.DistanceTravelled += length;
            foreach (var rec in _mapCollisions)
            {
                if (flashGrenade.Rectangle.Intersects(rec))
                    flashGrenade.Event.Enqueue(Enums.Events.ObjectHitted);
            }
        }

        private void TryShoot(IGameObject gameObject)
        {
            switch (gameObject)
            {
                case Player player: TryShoot(player); break;
            }
        }

        private void TryShoot(Player player)
        {
            Bullet bullet = null;
            if (player == null) return;
            var shootLoc = VectorTool.ExtendVector(player.ShootLocation, player.Location, 100000);
            bullet = Shoot.PlayerShoot(player, _gameTime, shootLoc);
            if (bullet == null) { return; }
            bullet.Direction = shootLoc - player.Location;
            bullet.Direction.Normalize();
            _gameObjects.Add(bullet);
        }

        private void ObjectHitted(IGameObject gameObject)
        {
            if (gameObject is Bullet bullet) ObjectHitted(bullet);
            if (gameObject is HandGrenade hgrenade) ObjectHitted(hgrenade);
            if (gameObject is FlashGrenade fgrenade) ObjectHitted(fgrenade);
        }

        private void ObjectHitted(Bullet bullet)
        {
            if (bullet.ObjectHitted?.GetType() == typeof(Player))
            {
                DealDamage.DealDamageToPlayer(bullet.ObjectHitted as Player, bullet.DamageDealt);
                CreateDamage(bullet, _gameTime);
            }
            DeleteObject.FromGameObjects(bullet, _gameObjects);
        }

        private void ObjectHitted(HandGrenade hGrenade) { }
        private void ObjectHitted(FlashGrenade fGrenade) { }

        private void Delete(IGameObject gameObject)
        {
            DeleteObject.FromGameObjects(gameObject, _gameObjects);
        }

        private void CreateDamage(Bullet bullet, GameTime gameTime)
        {
            Damage Damage = new Damage(bullet.DamageDealt, bullet.Location);
            Damage.Font = ContentContainer.DamageFont;
            Damage.Location = bullet.Location;
            Damage.ShowTime = gameTime.TotalGameTime.TotalMilliseconds;
            _sprites.Add(Damage);
        }
        private void CreateDamage(Bomb bomb, GameTime gameTime)
        {
            Damage Damage = new Damage(bomb.DamageDealt, bomb.Location);
            Damage.Font = ContentContainer.DamageFont;
            Damage.Location = bomb.Location;
            Damage.ShowTime = gameTime.TotalGameTime.TotalMilliseconds;
            _sprites.Add(Damage);
        }
        private void CreateDamage(HandGrenade handGrenade, Player player, GameTime gameTime)
        {
            Damage Damage = new Damage(handGrenade.DamageDealt, player.Location);
            Damage.Font = ContentContainer.DamageFont;
            Damage.Location = player.Location;
            Damage.ShowTime = gameTime.TotalGameTime.TotalMilliseconds;
            _sprites.Add(Damage);
        }

        private bool InRectangle(Rectangle rect)
        {
            foreach (var rec in _mapCollisions)
            {
                if (rect.Intersects(rec)) return true;
            }
            return false;
        }

    }
}
