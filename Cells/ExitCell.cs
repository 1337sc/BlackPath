using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using tgBot.EffectUtils;
using tgBot.Game;

namespace tgBot.Cells
{
    class ExitCell : Cell
    {
        public ExitCell(string name, string colour, 
            Figures figure, string figureColour, bool fill, 
            bool hasDialogue, Effect[] effects, string desc) : base(name, colour,
                figure, figureColour, fill, hasDialogue, effects, desc)
        {
            Type = CellTypes.Exit;
            Opened = false;
        }

        internal override void OnEnter(Player p)
        {
            Task.Run(() => GameCore.CheckAndSendAsync(p.Id, "You've reached the exit!")).Wait();
            p.EndGame();
        }

        internal override void OnGlance(Player p)
        {
            base.OnGlance(p);
        }
    }
}
