using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;

namespace tgBot
{
    public abstract class Serializable
    {
        protected virtual void OnSerialized() { }
        protected virtual void OnDeserialized() { }
        public async Task SerializeTo(FileStream fs)
        {
            foreach (var prop in from prop in GetType().GetProperties()
                                 where CheckAttributes(prop)
                                 select prop)
            {
                if (prop.PropertyType.IsArray && prop.PropertyType != typeof(string))
                {
                    await SerializeArray(fs, prop);
                }
                else
                {
                    object serializedValue = prop.PropertyType.IsEnum
                        ? (int)prop.GetValue(this) : prop.GetValue(this);
                    await GameDataProcessor.SerializeValueOfType(prop.PropertyType.IsEnum
                        ? typeof(int) : prop.PropertyType, fs, serializedValue);
                }
            }
            OnSerialized();
        }

        public async Task DeserializeFrom(FileStream fs)
        {
            foreach (var prop in from prop in GetType().GetProperties()
                                 where CheckAttributes(prop)
                                 select prop)
            {
                if (prop.PropertyType.IsArray && prop.PropertyType != typeof(string))
                {
                    await DeserializeArray(fs, prop);
                }
                else
                {
                    var deserializedVal = await GameDataProcessor.DeserializeValueOfType(
                        prop.PropertyType.IsEnum ? typeof(int) : prop.PropertyType, fs);
                    prop.SetValue(this, deserializedVal);
                }
            }
            OnDeserialized();
        }

        private bool CheckAttributes(PropertyInfo prop)
        {
            var serializedAttrs = prop.GetCustomAttributes(typeof(SerializedAttribute), false);
            return serializedAttrs.Length > 0;
        }

        private async Task SerializeArray(FileStream fs, PropertyInfo prop)
        {
            Array currentArrProp = (Array)prop.GetValue(this);
            await GameDataProcessor.SerializeValueOfType(currentArrProp.Rank.GetType(),
                fs, currentArrProp.Rank); //serialize array dimensity
            await GameDataProcessor.SerializeValueOfType(currentArrProp.GetLength(0).GetType(),
                fs, currentArrProp.GetLength(0)); //serialize array length 
            foreach (var item in currentArrProp)
            {
                await ((Serializable)item)?.SerializeTo(fs);
            }
        }

        private async Task DeserializeArray(FileStream fs, PropertyInfo prop)
        {
            Type arrElementType = prop.PropertyType.GetElementType();

            int currentArrPropRank = (int)await GameDataProcessor.DeserializeValueOfType(typeof(int), fs);
            int currentArrPropLength = (int)await GameDataProcessor.DeserializeValueOfType(typeof(int), fs);
            int[] arrayModel = new int[currentArrPropRank]; //save array lengths using serialized rank
            for (int i = 0; i < currentArrPropRank; i++)
            {
                arrayModel[i] = currentArrPropLength;
            }
            prop.SetValue(this, Array.CreateInstance(arrElementType, arrayModel));
            for (int i = 0; i < currentArrPropLength; i++)
            {
                for (int j = 0; j < currentArrPropLength; j++)
                {
                    var propInstance = Activator.CreateInstance(arrElementType);
                    await ((Serializable)propInstance)?.DeserializeFrom(fs);
                    ((Array)prop.GetValue(this)).SetValue(Convert.ChangeType(propInstance, arrElementType), i, j);
                }
            }
        }
    }
}
