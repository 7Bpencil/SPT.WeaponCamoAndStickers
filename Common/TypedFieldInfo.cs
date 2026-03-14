using System.Reflection;
using HarmonyLib;

namespace SevenBoldPencil.Common
{
    public struct TypedFieldInfo<I, F>
    {
        public FieldInfo Field;

        public TypedFieldInfo(string fieldName)
        {
            Field = AccessTools.Field(typeof(I), fieldName);
        }

        public void Set(I instance, F fieldValue)
        {
            Field.SetValue(instance, fieldValue);
        }

        public F Get(I instance)
        {
            return (F)Field.GetValue(instance);
        }
    }
}
