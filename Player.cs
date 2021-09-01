using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using tgBot.Cells;
using tgBot.EffectUtils;
using tgBot.Game;

namespace tgBot
{
    public enum PlayerModes
    {
        Glance,
        Move
    }

    public sealed class Player : ISerializable
    {
        bool ISerializable.IsDifferentForArrays { get; } = false;

        [DoNotSerialize]
        public long Id { get; set; }

        public Cell[,] Field { get; set; }

        public Cell.Figures Figure { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public int Money { get; set; }

        public int Points { get; set; }

        public int HP { get; set; }

        public int GlanceCount { get; set; }

        public int WalkDist { get; set; } = 1;
        public int GlanceDist { get; set; } = 1;
        public bool InvulnerableToDarkness { get; set; } = false;
        public bool InvulnerableToEnterTraps { get; set; } = false;
        public bool InvulnerableToGlanceTraps { get; set; } = false;

        public bool AskedDirection { get; set; }

        public bool AskedNeighbourhood { get; set; }

        public readonly int glanceCountMax = 2; //TODO: varying glances count depending on the game's difficulty
        public PlayerModes Mode = PlayerModes.Move;
        private const int emptyCellChance = 60; //a chance for the cell with type "Empty" to be put
                                                //on the field
        [DoNotSerialize]
        public DateTime TimeStamp { get; set; } //to delete inactive users
        [DoNotSerialize]
        public List<Cell> CellsList { get; } = new List<Cell>(); //list of a player's cells (mod support)
        /// <summary>
        /// List of all player's effects (mod support). Isn't serialized.
        /// </summary>
        [DoNotSerialize]
        public List<Effect> EffectsList { get; } = new List<Effect>(); 
        /// <summary>
        /// List of the effects applied to the player (for usage). Isn't serialized.
        /// </summary>
        [DoNotSerialize]
        public List<Effect> CurrentEffectsList { get; private set; } = new List<Effect>();
        
        /// <summary>
        /// Array of the effects applied to the player (for serialization)
        /// </summary>
        private Effect[] CurrentEffects
        {
            get => CurrentEffectsList.ToArray();
            set
            {
                foreach (var item in value)
                {
                    CurrentEffectsList.Add(item);
                }
            } 
        }
        private readonly Cell playerCell;
        //TODO: Items and their list
        //public List<Item> ItemsList { get; set; }

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
            Figure = (Cell.Figures)new Random().Next(1, 4);
            HP = 3;
            GlanceCount = glanceCountMax;
            TimeStamp = DateTime.Now;
            GameCore.GetPlayerResources(this);
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
        internal async void DirectAction(int deltaX, int deltaY)
        {
            switch (Mode)
            {
                case PlayerModes.Move:
                    await MoveAction(deltaX, deltaY);
                    break;
                case PlayerModes.Glance:
                    await GlanceAction(deltaX, deltaY);
                    break;
            }
            if (HP == 0)
            {
                await GameCore.CheckAndSendAsync(Id, $"You have lost all your health and died! Type /game to start a new game!");
                await GameCore.ResetPlayer(Id);
                EndGame();
            }
        }

        private async Task MoveAction(int deltaX, int deltaY)
        {
            int newX = X + WalkDist * deltaX;
            int newY = Y + WalkDist * deltaY;
            if (!await CheckCell(newX, newY))
            {
                return;
            }
            Field[X, Y] = CellsList.Find(x => x.Type == Cell.CellTypes.Darkness).Clone();
            Field[X, Y].Opened = true;
            X = newX;
            Y = newY;
            GlanceCount = glanceCountMax;
            Field[X, Y].OnEnter(this);
        }

        private async Task GlanceAction(int deltaX, int deltaY)
        {
            if (GlanceDist == 0)
            {
                await GameCore.CheckAndSendAsync(Id, $"I can't see anything!");
                return;
            }
            int newX = X + GlanceDist * deltaX;
            int newY = Y + GlanceDist * deltaY;
            if (!await CheckCell(newX, newY))
            {
                return;
            }
            if (GlanceCount > 0)
            {
                Field[newX, newY].Opened = true;
                GlanceCount--;
                Field[newX, newY].OnGlance(this);
                await GameCore.CheckAndSendAsync(Id, $"I see {Field[newX, newY].Name}");
            }
            else
            {
                await GameCore.CheckAndSendAsync(Id, $"I have to move, the Darkness is coming!");
            }
        }

        private async Task<bool> CheckCell(int coordX, int coordY)
        {
            if (coordX < 0 || coordX >= Field.GetLength(0) ||
                coordY < 0 || coordY >= Field.GetLength(1))
            {
                await GameCore.CheckAndSendAsync(Id, "You see nothing but a wall towards you...\n");
                return false;
            }
            if (Field[coordX, coordY].Type == Cell.CellTypes.Darkness)
            {
                await GameCore.CheckAndSendAsync(Id, "The darkness blocks the way there!");
                return false;
            }
            return true;
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

        internal void EndGame()
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
            GameCore.RemoveActivePlayer(Id);
            Task.Run(() => GameCore.CheckAndSendAsync(Id, $"You have collected {points:.2} point(s)!" +
                $" Type /game to start a new game.")).Wait();
        }

        internal bool CheckHonesty()
        {
            //TODO: when the Effects are done, check if the player has the effect that makes everyone honest
            if (Figure == Field[X, Y].Figure) return true;
            var rnd = new Random();
            return rnd.Next(0, 1) % 2 == 0;
        }
        /// <summary>
        /// Finds the position of the first occured Exit
        /// </summary>
        /// <returns>Exit position in a Tuple<X, Y></returns>
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

        void ISerializable.OnSerialized() { }

        void ISerializable.OnDeserialized() { }
    }
}