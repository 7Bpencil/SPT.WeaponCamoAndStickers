// Copyright (c) 2026 7Bpencil
// SPDX-License-Identifier: Apache-2.0

using System;

namespace SevenBoldPencil.Common
{
    public struct StringCache<T> where T : IEquatable<T>
    {
        public Func<T, string> Format;
        public T Value;
        public string ValueString;
        public bool HasValue;

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
