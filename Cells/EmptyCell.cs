using System;
using System.Collections.Generic;
using System.Text;
using tgBot.EffectUtils;

namespace tgBot.Cells
{
    class EmptyCell : Cell
    {
        public EmptyCell(string name, string colour,
            Figures figure, string figureColour,
            bool fill, bool hasDialogue,
            Effect[] enterEffects, Effect[] glanceEffects, string desc) : base(name: name,
                colour: colour, figure: figure,
                figureColour: figureColour,
                fill: fill, hasDialogue: hasDialogue,
                enterEffects: enterEffects, glanceEffects: glanceEffects, desc: desc)
        {
            Type = CellTypes.Empty;
            Opened = false;
        }

        internal override void OnEnter(Player p)
        {
            p.Money++;
            base.OnEnter(p);
        }

        internal override void OnGlance(Player p) { base.OnGlance(p); }
    }
}
