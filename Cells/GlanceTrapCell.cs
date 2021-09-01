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
            Effect[] enterEffects, Effect[] glanceEffects, string desc) : base(name, colour,
                figure, figureColour,
                fill, hasDialogue,
                enterEffects, glanceEffects, desc)
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
                p.GlanceCount = 0;
            }
            base.OnGlance(p);
        }
    }
}
