// Copyright (c) 2026 7Bpencil
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;

namespace SevenBoldPencil.Common
{
    public readonly struct Option<T>
    {
        public readonly T Value;
        public readonly bool HasValue;

        // do not use this constructor, its meant for serializers
        [JsonConstructor]
        public Option(T value, bool hasValue)
        {
            Value = value;
            HasValue = hasValue;
        }

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
