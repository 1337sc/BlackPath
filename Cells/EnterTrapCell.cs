using System;
using System.Collections.Generic;
using System.Text;
using tgBot.EffectUtils;

namespace tgBot.Cells
{
    class EnterTrapCell : Cell
    {
        public EnterTrapCell(string name, string colour,
            Figures figure, string figureColour, bool fill, bool hasDialogue,
            Effect[] enterEffects, Effect[] glanceEffects, string desc) : base(name, colour,
                figure, figureColour, fill, hasDialogue, enterEffects, glanceEffects, desc)
        {
            Type = CellTypes.EnterTrap;
            Opened = false;
        }


        internal override void OnEnter(Player p)
        {
            if (!p.InvulnerableToEnterTraps)
            {
                p.HP--;
            }
            base.OnEnter(p);
        }

        internal override void OnGlance(Player p) { base.OnGlance(p); }
    }
}
