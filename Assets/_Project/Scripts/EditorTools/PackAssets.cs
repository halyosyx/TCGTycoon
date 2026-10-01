using System;
using Game.Core.Content;
using Game.Unity.Definitions;
using UnityEditor;

namespace Game.EditorTools
{
    /// <summary>Shared editor helpers for finding pack assets and converting them to Core types.</summary>
    internal static class PackAssets
    {
        /// <summary>The in-print default pack, which the scene opens.</summary>
        public const string DefaultPackPath = "Assets/_Project/Data/Products/SetA_BoosterPack.asset";

        /// <summary>
        /// The default pack (<see cref="DefaultPackPath"/>), else the first pack configuration found, or
        /// null when there is none. With more than one pack the search order alone isn't a default.
        /// </summary>
        public static PackConfigDefinition FindDefault()
        {
            var defaultPack = AssetDatabase.LoadAssetAtPath<PackConfigDefinition>(DefaultPackPath);
            if (defaultPack != null)
            {
                return defaultPack;
            }

            string[] guids = AssetDatabase.FindAssets("t:" + nameof(PackConfigDefinition));
            return guids.Length == 0
                ? null
                : AssetDatabase.LoadAssetAtPath<PackConfigDefinition>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        /// <summary>Converts the pack's card set to a Core pool, or returns false with a readable reason.</summary>
        public static bool TryGetPool(PackConfigDefinition pack, out CardPool pool, out string error)
        {
            pool = null;
            error = null;
            if (pack == null)
            {
                error = "No pack selected.";
                return false;
            }

            if (pack.CardSet == null)
            {
                error = "The pack has no card set assigned.";
                return false;
            }

            try
            {
                pool = pack.CardSet.ToCardPool();
                return true;
            }
            catch (InvalidOperationException exception)
            {
                error = exception.Message;
                return false;
            }
            catch (ArgumentException exception)
            {
                error = exception.Message;
                return false;
            }
        }
    }
}
