using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using tgBot.Game;
using System.Collections.Generic;

namespace tgBot
{
    public interface ISerializable
    {
        protected bool IsDifferentForArrays { get; }
        protected virtual ISerializable GetArrayMemberToSetAfterDeserialized() =>
            IsDifferentForArrays ? throw new NotImplementedException() : this;
        protected abstract void OnSerialized();
        protected abstract void OnDeserialized();

        public async Task SerializeTo(FileStream fs)
        {
            var propsList = from prop in GetType().GetProperties(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            where CheckAttributes(prop)
                            select prop;
            foreach (var prop in propsList)
            {
                //await Logger.Log("Serializing: " + prop.Name);
                if (prop.PropertyType.IsArray && prop.PropertyType != typeof(string))
                {
                    try
                    {
                        await SerializeArray(fs, prop);
                    }
                    catch (Exception ex)
                    {
                        await Logger.Log(ex.Message);
                        continue;
                    }
                }
                else
                {
                    object serializedValue = prop.GetValue(this);
                    await GameCore.SerializeValueOfType(prop.PropertyType.IsEnum
                        ? typeof(int) : prop.PropertyType, fs, serializedValue);
                }
            }
            OnSerialized();
        }

        public async Task DeserializeFrom(FileStream fs)
        {
            var propsList = from prop in GetType().GetProperties(
                   BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            where CheckAttributes(prop)
                            select prop;
            foreach (var prop in propsList)
            {
                //await Logger.Log("Deserializing: " + prop.Name);
                if (prop.PropertyType.IsArray && prop.PropertyType != typeof(string))
                {
                    try
                    {
                        await DeserializeArray(fs, prop);
                    }
                    catch (Exception ex)
                    {
                        await Logger.Log(ex.Message);
                        throw;
                    }
                }
                else
                {
                    var deserializedVal = await GameCore.DeserializeValueOfType(
                        prop.PropertyType.IsEnum ? typeof(int) : prop.PropertyType, fs);
                    prop.SetValue(this, deserializedVal);
                }
                //await Logger.Log("\tValue: " + prop.GetValue(this).ToString());
            }
            OnDeserialized();
        }

        private async Task SerializeArray(FileStream fs, PropertyInfo prop)
        {
            Array currentArrProp = (Array)prop.GetValue(this);
            if (currentArrProp == null)
            {
                var currentPropType = prop.PropertyType;
                var rankArray = new int[currentPropType.GetArrayRank()];

                currentArrProp = Array.CreateInstance(currentPropType.GetElementType(),
                    rankArray);
            }

            await GameCore.SerializeValueOfType(currentArrProp.Rank.GetType(),
                fs, currentArrProp.Rank); //serialize array dimensity
            await GameCore.SerializeValueOfType(currentArrProp.GetLength(0).GetType(),
                fs, currentArrProp.GetLength(0)); //serialize array length 

            foreach (var item in currentArrProp)
            {
                await ((ISerializable)item)?.SerializeTo(fs);
            }
        }

        private async Task DeserializeArray(FileStream fs, PropertyInfo prop)
        {
            Type arrElementType = prop.PropertyType.GetElementType();

            int currentArrPropRank = (int)await GameCore.DeserializeValueOfType(typeof(int), fs);
            int currentArrPropLength = (int)await GameCore.DeserializeValueOfType(typeof(int), fs);

            if (currentArrPropRank == 0)
            {
                await Logger.Log($"Array rank for {prop.Name} appeared to be zero: " +
                    $"either the player hasn't registered before or something went wrong with serialization");
                return;
            }

            int[] arrayModel = new int[currentArrPropRank]; //save array lengths using serialized rank
            for (int i = 0; i < currentArrPropRank; i++)
            {
                arrayModel[i] = currentArrPropLength;
            }
            prop.SetValue(this, Array.CreateInstance(arrElementType, arrayModel));

            RecurseNestedLoops(currentArrPropRank, currentArrPropLength,
                async (indices) =>
                {
                    object propInstance;
                    try
                    {
                        propInstance = Activator.CreateInstance(arrElementType);
                    }
                    catch (Exception ex)
                    {
                        await Logger.Log(ex.Message + ex.StackTrace);
                        return;
                    }
                    (propInstance as ISerializable)?.DeserializeFrom(fs).Wait();
                    ((Array)prop.GetValue(this)).SetValue(((ISerializable)propInstance)
                        ?.GetArrayMemberToSetAfterDeserialized(), indices);
                }
            );
        }

        private void Test(PropertyInfo prop, object propInstance)
        {
            ((Array)prop.GetValue(this)).SetValue(new EffectUtils.Effect(), 0);
        }

        /// <summary>
        /// Create a needed amount of nested loops inside each other
        /// </summary>
        /// <param name="nestLevel">Count of nested loops to be performed</param>
        /// <param name="iterationsCount">Number of iterations each loop should perform</param>
        /// <param name="payload">An action to be performed every innermost loop iteration</param>
        private void RecurseNestedLoops(int nestLevel, int iterationsCount, Action<int[]> payload, params int[] previousIndices)
        {
            if (previousIndices == null)
            {
                previousIndices = Array.Empty<int>();
            }
            for (int i = 0; i < iterationsCount; i++)
            {
                if (nestLevel > 1)
                {
                    RecurseNestedLoops(nestLevel - 1, iterationsCount, payload, previousIndices.Append(i).ToArray());
                    continue;
                }
                payload.Invoke(previousIndices.Append(i).ToArray());
            }
        }

        private static bool CheckAttributes(PropertyInfo prop)
        {
            var serializedAttrs = prop.GetCustomAttributes(typeof(DoNotSerializeAttribute), false);
            return serializedAttrs.Length == 0;
        }
    }
}
