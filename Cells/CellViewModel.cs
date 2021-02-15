using System;
using System.Collections.Generic;
using System.Text;
using tgBot.EffectUtils;

namespace tgBot.Cells
{
    public class CellViewModel : Cell
    {
        public CellViewModel(string name,
            string colour,
            Figures figure,
            string figureColour,
            bool fill,
            bool hasDialogue,
            Effect[] effects,
            string desc) : base(name,
                colour,
                figure,
                figureColour,
                fill,
                hasDialogue,
                effects, desc)
        {
        }
    }
}
