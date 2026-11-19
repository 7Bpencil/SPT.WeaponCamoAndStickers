// Copyright (c) 2026 7Bpencil
// SPDX-License-Identifier: Apache-2.0

namespace SevenBoldPencil.Common
{
    public readonly struct Option<T>
    {
        public readonly T Value;
        public readonly bool HasValue;

        public Option(T value)
        {
            Value = value;
            HasValue = true;
        }

        public bool Some(out T value)
        {
            value = Value;
            return HasValue;
        }

        public T Else(T defaultValue)
        {
            return HasValue ? Value : defaultValue;
        }
    }
}
