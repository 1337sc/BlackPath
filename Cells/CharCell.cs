using System;
using System.Collections.Generic;
using System.Text;
using tgBot.EffectUtils;
using tgBot.Game;

namespace tgBot.Cells
{
    class CharCell : Cell
    {
        public CharCell(string name, string colour,
            Figures figure, string figureColour, bool fill,
            bool hasDialogue, Effect[] enterEffects, Effect[] glanceEffects, string desc) : base(name, colour,
                figure, figureColour, fill, hasDialogue, enterEffects, glanceEffects, desc)
        {
            Type = CellTypes.Char;
            Opened = false;
        }


        internal override void OnEnter(Player p)
        {
            GameCore.AddPlayerInDialogue(p);
            base.OnEnter(p);
        }

        internal override void OnGlance(Player p) { base.OnGlance(p); }
    }
}
