using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Threading.Tasks;

namespace tgBot.Cells
{
    public abstract partial class Cell : Serializable
    {
        private const int minRndColor = 100;
        private const int maxRndColor = 200;
        public const int CellSize = 40;
        public const int BorderSize = 2;
        public const string FogCellColor = "EFEFEF"; //the color for the cells the player hasn't seen

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

        public static readonly Dictionary<string, Figures> FiguresDict =
            new Dictionary<string, Figures>()
            {
                ["none"] = Figures.None,
                ["square"] = Figures.Square,
                ["circle"] = Figures.Circle,
                ["triangle"] = Figures.Triangle
            };

        public string Name { get; set; } //any unique

        public CellTypes Type { get; set; } //empty, glance_trap, enter_trap, char, player, darkness, exit

        public string Colour { get; set; } //background color, like "123abc"

        public Figures Figure { get; set; } //triangle, circle, square, none

        public string FigureColour { get; set; } //like "123abc" or "random"

        public bool Fill { get; set; } //true, false

        public bool HasDialogue { get; set; } //true, false

        public string Effect { get; set; } //none, positive, negative, neutral, "name;Effect name from table" [;probability]

        public string Desc { get; set; } //any

        public bool Opened { get; set; } //has the cell been glanced onto or visited

        protected Cell(string name, string colour,
            Figures figure, string figureColour,
            bool fill, bool hasDialogue,
            string effect, string desc)
        {

            Name = name;
            Colour = colour;
            Figure = figure;
            FigureColour = figureColour;
            Fill = fill;
            HasDialogue = hasDialogue;
            Effect = effect;
            Desc = desc;
        }

        public static Cell CreateCell(CellTypes type, string name, string colour,
            Figures figure, string figureColour,
            bool fill, bool hasDialogue,
            string effect, string desc)
        {
            switch (type)
            {
                case CellTypes.ErrType:
                    Task.Run(() => Logger.Log("Could not get cell type - it's been set to Empty")).Wait();
                    return new EmptyCell(name: name, colour: colour,
                        figure: figure, figureColour: figureColour,
                        fill: fill, hasDialogue: hasDialogue,
                        effect: effect, desc: desc);
                case CellTypes.Empty:
                    return new EmptyCell(name: name, colour: colour,
                        figure: figure, figureColour: figureColour,
                        fill: fill, hasDialogue: hasDialogue,
                        effect: effect, desc: desc);
                case CellTypes.GlanceTrap:
                    return new GlanceTrapCell(name: name, colour: colour,
                        figure: figure, figureColour: figureColour,
                        fill: fill, hasDialogue: hasDialogue,
                        effect: effect, desc: desc);
                case CellTypes.EnterTrap:
                    return new EnterTrapCell(name: name, colour: colour,
                        figure: figure, figureColour: figureColour,
                        fill: fill, hasDialogue: hasDialogue,
                        effect: effect, desc: desc);
                case CellTypes.Char:
                    return new CharCell(name: name, colour: colour,
                        figure: figure, figureColour: figureColour,
                        fill: fill, hasDialogue: hasDialogue,
                        effect: effect, desc: desc);
                case CellTypes.Player:
                    return new PlayerCell(name: name, colour: colour,
                        figure: figure, figureColour: figureColour,
                        fill: fill, hasDialogue: hasDialogue,
                        effect: effect, desc: desc);
                case CellTypes.Darkness:
                    return new DarknessCell(name: name, colour: colour,
                        figure: figure, figureColour: figureColour,
                        fill: fill, hasDialogue: hasDialogue,
                        effect: effect, desc: desc);
                case CellTypes.Exit:
                    return new ExitCell(name: name, colour: colour,
                        figure: figure, figureColour: figureColour,
                        fill: fill, hasDialogue: hasDialogue,
                        effect: effect, desc: desc);
                default:
                    Task.Run(() => Logger.Log("Unrecognizable cell type - it's been set to Empty")).Wait();
                    return new EmptyCell(name: name, colour: colour,
                        figure: figure, figureColour: figureColour,
                        fill: fill, hasDialogue: hasDialogue,
                        effect: effect, desc: desc);
            }
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
            if (clone is Cell cell)
            {
                return cell;
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

        internal virtual void OnEnter(Player p)
        {
            GameInterfaceProcessor.AskForActionAdapter(p);
        }
        internal virtual void OnGlance(Player p)
        {
            GameInterfaceProcessor.AskForActionAdapter(p);
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
        protected override void OnSerialized() { }
        protected override void OnDeserialized() { }
    }
}
