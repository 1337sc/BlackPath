using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace tgBot
{
    public class Cell : Serializable
    {
        private const int minRndColor = 100;
        private const int maxRndColor = 200;
        public const int CellSize = 40;
        public const int BorderSize = 2;
        public const string FogCellColor = "EFEFEF"; //the color for the cells the player hasn't seen
        public enum CellTypes
        {
            ErrType,
            Empty,
            GlanceTrap,
            EnterTrap,
            Char,
            Player,
            Darkness,
            Exit
        }
        public static readonly Dictionary<string, CellTypes> CellTypesDict =
            new Dictionary<string, CellTypes>()
            {
                ["empty"] = CellTypes.Empty,
                ["glance_trap"] = CellTypes.GlanceTrap,
                ["enter_trap"] = CellTypes.EnterTrap,
                ["char"] = CellTypes.Char,
                ["player"] = CellTypes.Player,
                ["darkness"] = CellTypes.Darkness,
                ["exit"] = CellTypes.Exit
            };
        public enum Figures
        {
            None,
            Triangle,
            Circle,
            Square,
        }
        public static readonly Dictionary<string, Figures> FiguresDict =
            new Dictionary<string, Figures>()
            {
                ["none"] = Figures.None,
                ["square"] = Figures.Square,
                ["circle"] = Figures.Circle,
                ["triangle"] = Figures.Triangle
            };
        [Serialized]
        public string Name { get; set; } //any unique
        [Serialized]
        public CellTypes Type { get; set; } //empty, glance_trap, enter_trap, char, player, darkness, exit
        [Serialized]
        public string Colour { get; set; } //background color, like "123abc"
        [Serialized]
        public Figures Figure { get; set; } //triangle, circle, square, none
        [Serialized]
        public string FigureColour { get; set; } //like "123abc" or "random"
        [Serialized]
        public bool Fill { get; set; } //true, false
        [Serialized]
        public bool HasDialogue { get; set; } //true, false
        [Serialized]
        public string Effect { get; set; } //none, positive, negative, neutral, "name;Effect name from table" [;probability]
        [Serialized]
        public string Desc { get; set; } //any
        [Serialized]
        public bool Opened { get; set; } //has the cell been glanced onto or visited
        public ActionHandlersPriorityController EnterActions { get; private set; } = new ActionHandlersPriorityController();
        public ActionHandlersPriorityController GlanceActions { get; private set; } = new ActionHandlersPriorityController();
        
        public Cell()
        {
        }

        public override string ToString()
        {
            return $"Name: {Name}\n" +
                $"Type: {Type}\n" +
                $"Colour: {Colour}\n" +
                $"Figure: {Figure}\n" +
                $"FigureColour: {FigureColour}\n" +
                $"Fill: {Fill}\n" +
                $"Dialogue: {HasDialogue}\n" +
                $"Effect: {Effect}\n" +
                $"Desc: {Desc}\n";
        }
        public Cell Clone()
        {
            var clone = MemberwiseClone();
            if (clone is Cell)
            {
                return (Cell)clone;
            }
            return null;
        }
        public Bitmap DrawCell()
        {
            var res = new Bitmap(CellSize, CellSize, PixelFormat.Format24bppRgb);
            var curColor = ConvertToColor(Opened ? Colour : FogCellColor);
            using (var graphics = Graphics.FromImage(res))
            {
                if (!Opened)
                {
                    graphics.Clear(curColor);
                    return res;
                }
                if (Fill)
                    graphics.Clear(curColor);
                else
                    graphics.Clear(ConvertToColor(FogCellColor));
                using var figBrush = new SolidBrush(ConvertToColor(FigureColour));
                switch (Figure)
                {
                    case Figures.Triangle:
                        graphics.FillPolygon(figBrush, new Point[] {
                            new Point(CellSize / 2, CellSize / 4),
                            new Point(CellSize / 4, CellSize * 3 / 4),
                            new Point(CellSize * 3 / 4, CellSize * 3 / 4),
                        });
                        break;
                    case Figures.Circle:
                        graphics.FillEllipse(figBrush, CellSize / 4, CellSize / 4, CellSize / 2, CellSize / 2);
                        break;
                    case Figures.Square:
                        graphics.FillRectangle(figBrush, CellSize / 4, CellSize / 4, CellSize / 2, CellSize / 2);
                        break;
                    default:
                        break;
                }
            }
            return res;
        }
        public void SetCellActions()
        {
            EnterActions.AddHandler(GameInterfaceProcessor.AskForActionAdapter, 
                int.MaxValue, nameof(GameInterfaceProcessor.AskForActionAdapter));
            GlanceActions.AddHandler(GameInterfaceProcessor.AskForActionAdapter, 
                int.MaxValue, nameof(GameInterfaceProcessor.AskForActionAdapter));
            switch (Type)
            {
                case CellTypes.Empty:
                    EnterActions.AddHandler(delegate (Player p)
                    {
                        p.Money++; 
                    });
                    break;
                case CellTypes.GlanceTrap:
                    EnterActions.AddHandler(delegate (Player p)
                    {
                        p.HP--;
                    });
                    break;
                case CellTypes.EnterTrap:
                    EnterActions.AddHandler(delegate (Player p)
                    {
                        p.HP--;
                    });
                    break;
                case CellTypes.Char:
                    EnterActions.AddHandler(delegate (Player p)
                    {
                        GameDataProcessor.AddPlayerInDialogue(p);
                    });
                    break;
                case CellTypes.Exit:
                    EnterActions.RemoveHandler(nameof(GameInterfaceProcessor.AskForActionAdapter));
                    EnterActions.AddHandler(async delegate (Player p)
                    {
                        await GameInterfaceProcessor.CheckAndSendAsync(p.Id, "You've reached the exit!");
                        p.EndGame();
                    });
                    break;
                default:
                    break;
            }
        }

        internal void OnEnter(Player p)
        {
            EnterActions?.DoActions(p);
        }
        internal void OnGlance(Player p)
        {
            GlanceActions?.DoActions(p);
        }
        protected void Cell_OnDeserialized()
        {
            SetCellActions();
        }
        private Color ConvertToColor(string hexFormat)
        {
            if (hexFormat == "random")
            {
                Random rnd = new Random();
                return Color.FromArgb(rnd.Next(minRndColor, maxRndColor),
                    rnd.Next(minRndColor, maxRndColor),
                    rnd.Next(minRndColor, maxRndColor));
            }
            return Color.FromArgb(int.Parse(hexFormat.Substring(0, 2), NumberStyles.HexNumber), int.Parse(hexFormat.Substring(2, 2), NumberStyles.HexNumber), int.Parse(hexFormat.Substring(4, 2), NumberStyles.HexNumber));
        }
        protected override void OnDeserialized()
        {
            SetCellActions();
        }
    }
}
