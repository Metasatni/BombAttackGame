﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace BombAttackGame.Interfaces
{
    public interface IGameObject
    {
        public Vector2 Location {get; set;}
        public Texture2D Texture { get; set;}
        public bool IsDead { get; set;}
        public Color Color { get; set;}
        public void Tick(GameTime GameTime, List<IGameObject> GameObjects) { }
        public Rectangle Rectangle { get; set;}
        public bool IsVisible { get; set;}
    }
}
