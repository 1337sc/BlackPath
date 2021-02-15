using System;
using System.Collections.Generic;
using System.Text;
using tgBot.EffectUtils;

namespace tgBot.Cells
{
    class GlanceTrapCell : Cell
    {
        public GlanceTrapCell(string name, string colour,
            Figures figure, string figureColour,
            bool fill, bool hasDialogue,
            Effect[] effects, string desc) : base(name, colour,
                figure, figureColour,
                fill, hasDialogue,
                effects, desc)
        {
            Type = CellTypes.GlanceTrap;
            Opened = false;
        }


        internal override void OnEnter(Player p) { base.OnEnter(p); }

        internal override void OnGlance(Player p)
        {
            if (!p.InvulnerableToGlanceTraps)
            {
                p.HP--;
            }
            base.OnGlance(p);
        }
    }
}
