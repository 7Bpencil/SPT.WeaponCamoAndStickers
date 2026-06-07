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
			try
			{
	            var fileInfo = new FileInfo(filePath);
	            Directory.CreateDirectory(fileInfo.Directory.FullName);
	            File.WriteAllTextAsync(fileInfo.FullName, text);
				return default;
			}
			catch (Exception e)
			{
				return new(e);
			}
		}

        public static string[] GetFiles(string directoryPath, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (!Directory.Exists(directoryPath))
            {
                return [];
            }
            try
            {
                return Directory.GetFiles(directoryPath, searchPattern, searchOption);
            }
            catch
            {
                return [];
            }
        }

        public static string[] GetDirectories(string directoryPath, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (!Directory.Exists(directoryPath))
            {
                return [];
            }
            try
            {
                return Directory.GetDirectories(directoryPath, searchPattern, searchOption);
            }
            catch
            {
                return [];
            }
        }

		public static char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
		public static bool IsValidFileName(string fileName)
		{
		    if (string.IsNullOrWhiteSpace(fileName))
			{
		        return false;
			}
			if (fileName.IndexOfAny(InvalidFileNameChars) >= 0)
			{
				return false;
			}
			if (fileName.StartsWith(" "))
			{
				return false;
			}

			return true;
		}
	}
}
