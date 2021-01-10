using System;
using System.Collections.Generic;
using System.Text;

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
            string effect,
            string desc) : base(name,
                colour,
                figure,
                figureColour,
                fill,
                hasDialogue,
                effect,
                desc)
        {
        }
    }
}
