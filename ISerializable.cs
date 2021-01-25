using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using tgBot.Game;

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
                    await GameCore.SerializeValueOfType(prop.PropertyType.IsEnum
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
                    var deserializedVal = await GameCore.DeserializeValueOfType(
                        prop.PropertyType.IsEnum ? typeof(int) : prop.PropertyType, fs);
                    prop.SetValue(this, deserializedVal);
                }
            }
            OnDeserialized();
        }

        private async Task SerializeArray(FileStream fs, PropertyInfo prop)
        {
            Array currentArrProp = (Array)prop.GetValue(this);
            if (currentArrProp == null)
            {
                currentArrProp = (Array)Activator.CreateInstance(prop.PropertyType);
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
                    object propInstance;
                    try
                    {
                        propInstance = Activator.CreateInstance(arrElementType);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex.Message + ex.StackTrace).Wait();
                        return;
                    }
                    await (propInstance as ISerializable)?.DeserializeFrom(fs);
                    ((Array)prop.GetValue(this)).SetValue((propInstance as ISerializable)
                        ?.GetArrayMemberToSetAfterDeserialized(), i, j);
                }
            }
        }

        private bool CheckAttributes(PropertyInfo prop)
        {
            var serializedAttrs = prop.GetCustomAttributes(typeof(DoNotSerializeAttribute), false);
            return serializedAttrs.Length == 0;
        }

    }
}
