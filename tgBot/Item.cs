using System;
using System.Collections.Generic;
using System.Text;
using tgBot.EffectUtils;

namespace tgBot
{
    public class Item : ISerializable
    {
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string Desc { get; set; }
        public List<Effect> GivenEffects { get; set; }

        bool ISerializable.IsDifferentForArrays => false;

        public Item(string name, string symbol, string desc, List<Effect> givenEffects)
        {
            Name = name;
            Symbol = symbol;
            Desc = desc;
            GivenEffects = givenEffects;
        }
    }
}
