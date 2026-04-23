//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using System;
using System.IO;

namespace SevenBoldPencil.Common
{
	public static class SafeIO
	{
		public static Result<string> ReadAllText(string filePath)
		{
            if (!File.Exists(filePath))
			{
				return new(new FileNotFoundException(filePath));
			}
            try
            {
				return new(File.ReadAllText(filePath));
            }
            catch (Exception e)
            {
				return new(e);
            }
		}

		public static Option<Exception> DeleteFile(string filePath)
		{
            try
            {
				File.Delete(filePath);
				return default;
            }
            catch (Exception e)
            {
				return new(e);
            }
		}

		public static Option<Exception> WriteAllTextAsync(string filePath, string text)
		{
            var fileInfo = new FileInfo(filePath);
			return WriteAllTextAsync(fileInfo, text);
		}

		public static Option<Exception> WriteAllTextAsync(FileInfo fileInfo, string text)
		{
			try
			{
	            Directory.CreateDirectory(fileInfo.Directory.FullName);
	            File.WriteAllTextAsync(fileInfo.FullName, text);
				return default;
			}
			catch (Exception e)
			{
				return new(e);
			}
		}

        public static string[] GetFiles(string directoryPath, string searchPattern = "*")
        {
            if (!Directory.Exists(directoryPath))
            {
                return [];
            }
            try
            {
                return Directory.GetFiles(directoryPath, searchPattern);
            }
            catch
            {
                return [];
            }
        }

        public static string[] GetDirectories(string directoryPath, string searchPattern = "*")
        {
            if (!Directory.Exists(directoryPath))
            {
                return [];
            }
            try
            {
                return Directory.GetDirectories(directoryPath, searchPattern);
            }
            catch
            {
                return [];
            }
        }
	}
}
