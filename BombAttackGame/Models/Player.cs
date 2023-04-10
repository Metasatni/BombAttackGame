﻿using BombAttackGame.Bonuses;
using BombAttackGame.Enums;
using BombAttackGame.Events;
using BombAttackGame.Interfaces;
using BombAttackGame.Vector;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BombAttackGame.Models
{
    public enum Team
    {
        Player,
        TeamMate,
        Enemy
    }
    internal class Player
    {
        public Vector2 Location { get; set; }
        public List<Vector2> Collision { get; set; }
        public Direction Direction { get; set; }
        public Team Team { get; set; }
        public int Health { get; set; }
        public double Speed { get; set; }
        public Texture2D Texture { get; set; }
        public bool IsDead { get; set; }
        public bool IsAttacked { get; set; }
        public bool OnMainSpeed { get; set; }
        public double ShotTime { get; set; }
        public double ShotLatency { get; set; }
        public double MainSpeedStartTime { get; set; }
        public double MainSpeedEndTime { get; set; }

        public Player() { 
        
            this.Direction = Direction.Right;
            this.Speed = 2;
            this.ShotLatency = 100;
            this.Health = 100;
            this.OnMainSpeed = false;
        }
        public void Hit(int Damage)
        {
            Health -= Damage;
        }

        public static void TryShoot(Player Player, GameTime GameTime, Microsoft.Xna.Framework.Content.ContentManager Content, Vector2 ShootLoc, List<Bullet> Bullets)
        {
            Bullet Bullet = null;
            ShootLoc = VectorTool.ExtendVector(ShootLoc, Player.Location, 100000); 
            Bullet = Shoot.PlayerShoot(Player, GameTime, Content, ShootLoc);
            if (Bullet == null) { return; }
            Bullets.Add(Bullet);
            Bullet.Direction = ShootLoc - Player.Location;
            Bullet.Direction.Normalize();
        }
        public static Player AddPlayer(Team Team, ContentManager Content, int[] MapSize)
        {
            string Texture = Team.ToString();
            Player Player = new Player();
            Player.Texture = Content.Load<Texture2D>(Texture);
            Player.Team = Team;
            Player.Location = Spawn.GenerateRandomSpawnPoint(MapSize, Player.Texture);
            return Player;
        }
        public static List<Player> AddPlayers(Team Team, ContentManager Content, int Amount, int[] MapSize)
        {
            List<Player> Players = new List<Player>();
            string Texture = Team.ToString();
            for (int i = 0; i < Amount; i++)
            {
                Player Player = new Player();
                Player.Texture = Content.Load<Texture2D>(Texture);
                Player.Team = Team;
                Player.Location = Spawn.GenerateRandomSpawnPoint(MapSize, Player.Texture);
                Player.Collision = VectorTool.Collision(Player.Location, Player.Texture);
                Players.Add(Player); 
            }
            return Players;
        }
        public static void Tick(List<Player> Players, MainSpeed MainSpeed, GameTime GameTime)
        {
            foreach (var player in Players.ToList())
            {
                if(CheckIfDead(player)) Players.Remove(player);
                UpdateCollision(player);
                if (VectorTool.IsOnObject(player.Collision, MainSpeed.Collision))
                { MainSpeed.PickedBonus(player,MainSpeed, GameTime); }
                if(player.OnMainSpeed) MainSpeedTime(player, GameTime, MainSpeed);
            }
        }
        private static bool CheckIfDead(Player Player)
        {
            if (Player.Health <= 0) return true;
            return false;
        } 
        public static void UpdateCollision(Player Player)
        {
            Player.Collision = VectorTool.Collision(Player.Location, Player.Texture);
        }
        public static void MainSpeedTime(Player Player, GameTime GameTime, MainSpeed MainSpeed)
        {
            if(Player.MainSpeedEndTime <= GameTime.TotalGameTime.TotalMilliseconds && Player.OnMainSpeed)
            {
                Player.Speed /= MainSpeed.WalkSpeed;
                Player.ShotLatency *= MainSpeed.ShootingSpeed;
                Player.OnMainSpeed = false;
            } 
        }
    }
}
