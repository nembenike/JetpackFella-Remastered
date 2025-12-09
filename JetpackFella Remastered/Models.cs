using System.Windows;


namespace JetpackFella_Remastered
{
    public enum GameState { Menu, Playing, GameOver }


    public class Player
    {
        public Point Position;
        public double Gravity;
        public bool IsJetpacking;
        public double Size = 20;


        public Rect Rect => new Rect(Position.X, Position.Y, Size, Size);
    }


    public class Asteroid
    {
        public Rect Rect;
        public Vector Speed;
        public double Rotation;
    }


    public class Pickup
    {
        public Rect Rect;
        public bool IsSpawned;
    }


    public class FuelSystem
    {
        public double Current;
        public int Maximum;
    }


    public class GameResources
    {
        public int Score;
    }
}