using System.IO;
using System.Text;
using UnityEditor;

namespace Game.EditorTools
{
    /// <summary>
    /// Writes generated text assets (USS, UXML, Markdown) only when their content changes, so a
    /// generator that runs again with the same inputs leaves the file, its import and git untouched.
    /// Content is compared without carriage returns: git may check files out with CRLF line endings.
    /// </summary>
    public static class GeneratedTextFiles
    {
        private static readonly UTF8Encoding s_utf8WithoutBom = new UTF8Encoding(false);

        /// <summary>The file's text with CRLF turned into LF, or null when there is no file.</summary>
        public static string ReadNormalized(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n") : null;
        }

        /// <summary>True when the file exists and holds exactly <paramref name="content"/> (LF line endings).</summary>
        public static bool Matches(string path, string content) => ReadNormalized(path) == content;

        /// <summary>Writes and imports the file unless it already matches. Returns whether it was written.</summary>
        public static bool WriteIfChanged(string path, string content)
        {
            if (Matches(path, content))
            {
                return false;
            }

            File.WriteAllText(path, content, s_utf8WithoutBom);
            AssetDatabase.ImportAsset(path);
            return true;
        }
    }
}
