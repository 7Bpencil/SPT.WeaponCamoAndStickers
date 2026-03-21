//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

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
