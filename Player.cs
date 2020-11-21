using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace tgBot
{
    public enum PlayerModes
    {
        Glance,
        Move
    }

    public class Player : Serializable
    {
        public long Id { get; set; }
        [Serialized]
        public Cell[,] Field { get; set; }
        [Serialized]
        public Cell.Figures Figure { get; set; }
        [Serialized]
        public int X { get; set; }
        [Serialized]
        public int Y { get; set; }
        [Serialized]
        public int Money { get; set; }
        [Serialized]
        public int Points { get; set; }
        [Serialized]
        public int HP { get; set; }
        [Serialized]
        public int GlanceCount { get; set; }
        [Serialized]
        public bool AskedDirection { get; set; }
        [Serialized]
        public bool AskedNeighbourhood { get; set; }
        public readonly int glanceCountMax = 2; //TODO: varying glances count depending on the game's difficulty
        public PlayerModes Mode = PlayerModes.Move;
        private const int emptyCellChance = 60; //a chance for the cell with type "Empty" to be put
                                                //on the field

        public DateTime TimeStamp { get; set; } //to delete inactive users
        public List<Cell> CellsList { get; set; } //list of a player's cells (mod support)
        private readonly Cell playerCell;
        //TODO: Items, Effects and their lists
        //public List<Item> ItemsList { get; set; }
        //public List<Effect> EffectsList { get; set; }
        /// <summary>
        /// Serialization constructor
        /// </summary>
        public Player()
        {
        }

        public Player(long id)
        {
            Id = id;
            Money = 0;
            HP = 3;
            GlanceCount = glanceCountMax;
            TimeStamp = DateTime.Now;
            GameDataProcessor.GetPlayerResources(this);
            playerCell = CellsList.Find(x => x.Type == Cell.CellTypes.Player).Clone();
            playerCell.Opened = true;
        }
        internal void CreateField(int size)
        {
            Field = new Cell[size, size];
            var rnd = new Random();
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    var pos = rnd.Next() % CellsList.Count;
                    if (rnd.Next() % 100 < emptyCellChance)
                    {
                        Field[i, j] = CellsList.Find(x => x.Type == Cell.CellTypes.Empty).Clone();
                    }
                    else if (CellsList[pos].Type != Cell.CellTypes.Player &&
                        CellsList[pos].Type != Cell.CellTypes.Darkness &&
                        CellsList[pos].Type != Cell.CellTypes.Exit)
                    {
                        Field[i, j] = CellsList[pos].Clone();
                    }
                    else j--;
                }
            }
            X = rnd.Next() % size;
            Y = rnd.Next() % size;
            var exitX = 0;
            var exitY = 0;
            do
            {
                exitX = rnd.Next() % size;
                exitY = rnd.Next() % size;
            }
            while (Math.Sqrt(Math.Pow(X - exitX, 2) +
                Math.Pow(Y - exitY, 2)) < 4);
            Field[exitX, exitY] = CellsList.Find(x => x.Type == Cell.CellTypes.Exit);
            Field[X, Y] = CellsList.Find(x => x.Type == Cell.CellTypes.Player);
            Field[X, Y].Opened = true;
        }
        internal async void Move(int deltaX, int deltaY)
        {
            int newX = X + deltaX;
            int newY = Y + deltaY;
            if (newX < 0 || newX >= Field.GetLength(0) ||
                newY < 0 || newY >= Field.GetLength(1))
            {
                await GameInterfaceProcessor.CheckAndSendAsync(Id, "You see nothing but a wall towards you...\n");
                return;
            }
            if (Field[newX, newY].Type == Cell.CellTypes.Darkness)
            {
                await GameInterfaceProcessor.CheckAndSendAsync(Id, "The darkness blocks the way there!");
                return;
            }
            if (Mode == PlayerModes.Move)
            {
                Field[X, Y] = CellsList.Find(x => x.Type == Cell.CellTypes.Darkness).Clone();
                Field[X, Y].Opened = true;
                X = newX;
                Y = newY;
                GlanceCount = glanceCountMax;
                Field[X, Y].OnEnter(this);
            }
            if (Mode == PlayerModes.Glance)
            {
                if (GlanceCount > 0)
                {
                    Field[newX, newY].Opened = true;
                    GlanceCount--;
                    Field[newX, newY].OnGlance(this);
                    await GameInterfaceProcessor.CheckAndSendAsync(Id, $"I see {Field[newX, newY].Name}");
                }
                else
                {
                    await GameInterfaceProcessor.CheckAndSendAsync(Id,
                        $"I have to move, the Darkness is coming!");
                }
            }
            if (HP == 0)
            {
                await GameInterfaceProcessor.CheckAndSendAsync(Id, $"You have lost all your health and died! Type /game to start a new game!");
                await GameDataProcessor.ResetPlayer(Id);
                EndGame();
            }
        }
        public Bitmap DrawField()
        {
            var cornerOffset = Cell.BorderSize + Cell.CellSize;
            var res = new Bitmap(cornerOffset * Field.GetLength(0),
                cornerOffset * Field.GetLength(1),
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var borderBrush = new SolidBrush(Color.Black);
            using Graphics graphics = Graphics.FromImage(res);
            for (int j = 0; j < Field.GetLength(0); j++)
            {
                for (int i = 0; i < Field.GetLength(1); i++)
                {
                    //drawing grid parts
                    graphics.FillRectangle(borderBrush,
                        new Rectangle(new Point(j * cornerOffset, i * cornerOffset),
                        new Size(Cell.BorderSize, Cell.BorderSize))); //a corner
                    graphics.FillRectangle(borderBrush,
                        new Rectangle(new Point(j * cornerOffset + Cell.BorderSize, i * cornerOffset),
                        new Size(Cell.BorderSize, Cell.CellSize))); //horizontal plank
                    graphics.FillRectangle(borderBrush,
                        new Rectangle(new Point(j * cornerOffset, i * cornerOffset + Cell.BorderSize),
                        new Size(Cell.CellSize, Cell.BorderSize))); //vertical plank
                    //draw the inner cells
                    if (i == Y && j == X)
                    {
                        graphics.DrawImage(playerCell.DrawCell(),
                            new Point(j * cornerOffset + Cell.BorderSize, i * cornerOffset + Cell.BorderSize));
                    }
                    else
                    {
                        graphics.DrawImage(Field[j, i].DrawCell(),
                            new Point(j * cornerOffset + Cell.BorderSize, i * cornerOffset + Cell.BorderSize));
                    }
                }
            }
            return res;
        }
        public override string ToString()
        {
            return $"Player {Id}, Position: ({X}, {Y}), TimeStamp: {TimeStamp}";
        }

        public override bool Equals(object obj)
        {
            return obj is Player player &&
                   Id == player.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        internal async void EndGame()
        {
            var points = 0d;
            foreach (var c in Field)
            {
                if (c.Opened)
                    points++;
            }
            points = Field.Length / points;
            points += Money * 100;
            points += HP * 1000;
            await GameInterfaceProcessor.CheckAndSendAsync(Id, $"You have collected {points:%.2d} point(s)!" +
                $" Type /game to start a new game.");
        }

        internal bool CheckHonesty()
        {
            //TODO: when the Effects are done, check if the player has the effect that makes everyone honest
            if (Figure == Field[X, Y].Figure) return true;
            var rnd = new Random();
            return rnd.Next(0, 1) % 2 == 0;
        }
        /// <summary>
        /// Finds the position of the first occured Exit and returns its position in a Tuple<X, Y>
        /// </summary>
        /// <returns></returns>
        internal (int X, int Y) FindExitPosition()
        {
            for (int i = 0; i < Field.GetLength(0); i++)
            {
                for (int j = 0; j < Field.GetLength(1); j++)
                {
                    if (Field[i, j].Type == Cell.CellTypes.Exit) return (i, j);
                }
            }
            return (-1, -1);
        }
    }
}