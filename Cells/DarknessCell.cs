using System;
using System.Collections.Generic;
using System.Text;

namespace tgBot.Cells
{
    class DarknessCell : Cell
    {
        public DarknessCell(string name, string colour,
            Figures figure, string figureColour, bool fill,
            bool hasDialogue, string effect, string desc) : base(name, colour,
                figure, figureColour, fill, hasDialogue, effect, desc)
        {
            Type = CellTypes.Darkness;
            Opened = true;
        }

        internal override void OnEnter(Player p)
        {
            base.OnEnter(p);
        }

        internal override void OnGlance(Player p)
        {
            base.OnGlance(p);
        }
    }
}
