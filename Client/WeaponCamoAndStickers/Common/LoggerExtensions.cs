//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using BepInEx.Logging;
using UnityEngine;

namespace SevenBoldPencil.Common
{
    public static class LoggerExtensions
    {
#if LOG_ALL
        public const bool LogAll = true;
#else
        public const bool LogAll = false;
#endif

        public static void Log(this ManualLogSource logger, LogLevel level, string type, string message)
        {
            if (LogAll || level != LogLevel.Info)
            {
                logger.Log(level, $"[{type}] {message}");
            }
        }

        public static void Log<A>(this ManualLogSource logger, LogLevel level, string type, string message, A a)
        {
            if (LogAll || level != LogLevel.Info)
            {
                logger.Log(level, $"[{type}] {message}: {a}");
            }
        }

        public static void Log<A, B>(this ManualLogSource logger, LogLevel level, string type, string message, A a, B b)
        {
            if (LogAll || level != LogLevel.Info)
            {
                logger.Log(level, $"[{type}] {message}: {a}, {b}");
            }
        }

        public static void Log<A, B, C>(this ManualLogSource logger, LogLevel level, string type, string message, A a, B b, C c)
        {
            if (LogAll || level != LogLevel.Info)
            {
                logger.Log(level, $"[{type}] {message}: {a}, {b}, {c}");
            }
        }
    }
}
