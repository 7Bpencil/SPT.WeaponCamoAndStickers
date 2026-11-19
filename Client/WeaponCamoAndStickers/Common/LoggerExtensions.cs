// Copyright (c) 2026 7Bpencil
// SPDX-License-Identifier: Apache-2.0

using BepInEx.Logging;

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
