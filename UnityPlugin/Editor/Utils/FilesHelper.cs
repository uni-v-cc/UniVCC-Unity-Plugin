
using System.IO;
using System.Linq;

namespace UniVCC
{
    public static class FilesHelper
    {
        public static bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (name == "." || name == "..") return false;
            if (name.Contains('/') || name.Contains('\\') || name.IndexOfAny(Path.GetInvalidFileNameChars()) != -1) return false;
            return name.Length <= 255;
        }
    }
}