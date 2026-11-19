// Copyright (c) 2026 7Bpencil
// SPDX-License-Identifier: Apache-2.0

using System;

namespace SevenBoldPencil.Common
{
    public readonly struct Result<T>
    {
        public readonly T Value;
		public readonly Exception Exception;
        public readonly bool Success;

        public Result(T value)
        {
            Value = value;
			Exception = default;
            Success = true;
        }

        public Result(Exception exception)
        {
            Value = default;
			Exception = exception;
            Success = false;
        }

        public bool Ok(out T value, out Exception exception)
        {
            value = Value;
			exception = Exception;
            return Success;
        }
    }
}
