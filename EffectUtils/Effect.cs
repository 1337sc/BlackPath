using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace tgBot.EffectUtils
{
    public partial class Effect : ISerializable
    {
        bool ISerializable.IsDifferentForArrays { get; } = false;

        private static readonly Dictionary<string, ProgramParams> effectProgramParamsDictionary =
            new Dictionary<string, ProgramParams>()
            {
                [ProgramParams.HP.ToString().ToLower()] = ProgramParams.HP,
                [ProgramParams.Invincible.ToString().ToLower()] = ProgramParams.Invincible,
                [ProgramParams.GlanceDist.ToString().ToLower()] = ProgramParams.GlanceDist,
                [ProgramParams.WalkDist.ToString().ToLower()] = ProgramParams.WalkDist,
                [ProgramParams.DialogQuality.ToString().ToLower()] = ProgramParams.DialogQuality
            };
        private static readonly Dictionary<string, InvincibleParams> effectInvincibleParamsDictionary =
            new Dictionary<string, InvincibleParams>()
            {
                [InvincibleParams.Darkness.ToString().ToLower()] = InvincibleParams.Darkness,
                [InvincibleParams.Enter.ToString().ToLower()] = InvincibleParams.Enter,
                [InvincibleParams.Glance.ToString().ToLower()] = InvincibleParams.Glance
            };
        private static readonly Dictionary<string, DialogQualityParams> effectDialogQualityParamsDictionary =
            new Dictionary<string, DialogQualityParams>()
            {
                [DialogQualityParams.Disabled.ToString().ToLower()] = DialogQualityParams.Disabled,
                [DialogQualityParams.Random.ToString().ToLower()] = DialogQualityParams.Random
            };

        public string Name { get; set; }

        /// <summary>
        /// Params available: <br/>
        /// HP: =n, +n or -n<br/>
        /// Invincible: enter, glance or darkness - invincibility from traps of such types or darkness<br/>
        /// GlanceDist: n >= 0, where n is the number of cells. if 0, the player becomes blind for some time<br/>
        /// WalkDist: n > 0, alike GlanceDist<br/>
        /// DialogQuality: random or disabled - if random, the player answers not always the thing he
        ///    wanted to, if disabled, the player can`t speak and trade<br/>
        /// </summary>
        [DoNotSerialize]
        public string EffectProgram { get; set; }

        [DoNotSerialize]
        public bool IsDeadly { get; set; } //true, false - if false, the effect can`t kill the player even if HP is 0, but it will still reduce health if HP > 0

        [DoNotSerialize]
        public string DurationType { get; set; } //a number or "random"

        [DoNotSerialize]
        public int Duration { get; set; }
        
        public void ProcessEffect(Player p)
        {
            foreach (var part in EffectProgram.Replace(" ", string.Empty).Split(';'))
            {
                try
                {
                    ProcessEffectPart(part, p);
                }
                catch (Exception ex)
                {
                    Logger.Log($"(Called by {p.Id}) An effect could not be processed: {ex.Message} {ex.StackTrace}").Wait();
                    continue;
                }
            }
            Duration--;
        }

        private void ProcessEffectPart(string part, Player p)
        {
            var partMembers = part.Split(':'); //splits a part in two other parts: an action type and a program
            if (partMembers.Length != 2)
            {
                throw new ArgumentException("Too many part members for part " + part);
            }

            if (!effectProgramParamsDictionary.TryGetValue(partMembers[0].ToLower(), out ProgramParams programParam))
            {
                throw new ArgumentException("Couldn't find an action type " + partMembers[0].ToLower());
            }
            switch (programParam)
            {
                case ProgramParams.HP:
                    ChangePlayerHP(p, partMembers);
                    break;
                case ProgramParams.Invincible:
                    ChangePlayerInvulnerability(p, partMembers);
                    break;
                case ProgramParams.GlanceDist:
                    ChangePlayerGlanceDist(p, partMembers);
                    break;
                case ProgramParams.WalkDist:
                    ChangePlayerWalkDist(p, partMembers);
                    break;
                case ProgramParams.DialogQuality:
                    break;
                default:
                    throw new ArgumentException($"Unexpected parameter \"{programParam}\"");
            }
        }

        private void ChangePlayerHP(Player p, string[] partMembers)
        {
            char hpSign = partMembers[1][0];
            if (!int.TryParse(partMembers[1].Substring(1), out int hpValue))
            {
                throw new ArgumentException("Incorrect HP value: " + partMembers[1].Substring(1));
            }

            switch (hpSign)
            {
                case '=':
                    p.HP = hpValue;
                    break;
                case '+':
                    p.HP += hpValue;
                    break;
                case '-':
                    if (p.HP - hpValue <= 0)
                    {
                        p.HP = IsDeadly ? 0 : 1;
                    }
                    else
                    {
                        p.HP -= hpValue;
                    }
                    break;
                default:
                    throw new ArgumentException($"Unexpected sign: \"{hpSign}\"");
            }
        }
        private static void ChangePlayerInvulnerability(Player p, string[] partMembers)
        {
            if (!effectInvincibleParamsDictionary.TryGetValue(partMembers[1].ToLower(),
                                    out InvincibleParams invincibleParams))
            {
                throw new ArgumentException("Couldn't find an invincibility type " + partMembers[1].ToLower());
            }
            switch (invincibleParams)
            {
                case InvincibleParams.Enter:
                    p.InvulnerableToEnterTraps = true;
                    break;
                case InvincibleParams.Glance:
                    p.InvulnerableToGlanceTraps = true;
                    break;
                case InvincibleParams.Darkness:
                    p.InvulnerableToDarkness = true;
                    break;
                default:
                    break;
            }
        }

        private static void ChangePlayerGlanceDist(Player p, string[] partMembers)
        {
            if (!int.TryParse(partMembers[1], out int glanceDistValue))
            {
                throw new ArgumentException("Incorrect GlanceDist value: " + partMembers[1]);
            }
            p.GlanceDist = glanceDistValue;
        }

        private static void ChangePlayerWalkDist(Player p, string[] partMembers)
        {
            if (!int.TryParse(partMembers[1], out int walkDistValue))
            {
                throw new ArgumentException("Incorrect WalkDist value: " + partMembers[1]);
            }
            if (walkDistValue <= 0 || walkDistValue > p.Field.GetLength(0)) { }
            p.WalkDist = walkDistValue;
        }

        void ISerializable.OnSerialized()
        {
            throw new NotImplementedException();
        }

        void ISerializable.OnDeserialized()
        {
            throw new NotImplementedException();
        }
    }
}
