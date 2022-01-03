﻿using System;
using System.Collections.Generic;
using System.Text;
using tgBot.EffectUtils;

namespace tgBot
{
    class Item : ISerializable
    {
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string Desc { get; set; }
        public Effect GivenEffect { get; set; }

        bool ISerializable.IsDifferentForArrays => false;
    }
}
