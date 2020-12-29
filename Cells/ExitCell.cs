﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace tgBot.Cells
{
    class ExitCell : Cell
    {
        public ExitCell(string name, string colour, 
            Figures figure, string figureColour, bool fill, 
            bool hasDialogue, string effect, string desc) : base(name, colour, 
                figure, figureColour, fill, hasDialogue, effect, desc)
        {
            Type = CellTypes.Exit;
            Opened = false;
        }

        internal override void OnEnter(Player p)
        {
            Task.Run(() => GameInterfaceProcessor.CheckAndSendAsync(p.Id, "You've reached the exit!")).Wait();
            p.EndGame();
        }

        internal override void OnGlance(Player p)
        {
            base.OnGlance(p);
        }
    }
}