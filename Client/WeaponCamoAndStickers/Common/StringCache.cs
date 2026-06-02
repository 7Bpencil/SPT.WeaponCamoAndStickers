//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using System;

namespace SevenBoldPencil.Common
{
    public struct StringCache<T> where T : IEquatable<T>
    {
        private Func<T, string> Format; // use string interpolation instead of string.Format
        private T Value;
        private string ValueString;
        private bool HasValue;

        public StringCache(Func<T, string> format)
        {
            Format = format;
            HasValue = false;
        }

        public string Get(T value)
        {
            if (HasValue && Value.Equals(value))
            {
                return ValueString;
            }

            Value = value;
            ValueString = Format(value);
            HasValue = true;

            return ValueString;
        }
    }
}
