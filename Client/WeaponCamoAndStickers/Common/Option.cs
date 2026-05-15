//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

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
    }
}
