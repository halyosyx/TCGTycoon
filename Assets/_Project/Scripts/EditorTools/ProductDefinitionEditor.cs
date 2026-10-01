using Game.Core.Common;
using Game.Core.Store;
using Game.Unity.Definitions;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Inspector for <see cref="ProductDefinition"/>: shows only the fields the product type uses. A
    /// booster pack's card set and market price come from its pack configuration, so they're shown
    /// read-only; the resulting store price is shown for every type.
    /// </summary>
    [CustomEditor(typeof(ProductDefinition))]
    public sealed class ProductDefinitionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var product = (ProductDefinition)target;

            EditorGUILayout.PropertyField(serializedObject.FindProperty(ProductDefinition.IdField));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(ProductDefinition.TypeDisplayNameField));
            SerializedProperty type = serializedObject.FindProperty(ProductDefinition.TypeField);
            EditorGUILayout.PropertyField(type);

            bool isPack = type.enumValueIndex == (int)ProductType.BoosterPack;
            if (isPack)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(ProductDefinition.PackConfigField));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(ProductDefinition.CardSetField));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(ProductDefinition.PackCountField));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(ProductDefinition.BasePriceField), new GUIContent("Market Price Cents"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty(ProductDefinition.SupplierPercentField));
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(true))
            {
                if (isPack)
                {
                    EditorGUILayout.ObjectField("Card Set (from pack)", product.CardSet, typeof(CardSetDefinition), false);
                    EditorGUILayout.LongField("Market Price Cents (from pack)", product.MarketPriceCents);
                }

                long storeCents = StorePricing.UnitPriceCents(product.MarketPriceCents, product.SupplierPercent);
                EditorGUILayout.TextField("Store Price", Money.FormatDisplay(storeCents));
            }

            if (isPack && (product.PackConfig == null || product.CardSet == null))
            {
                EditorGUILayout.HelpBox("A booster pack needs a pack configuration with a card set.", MessageType.Error);
            }
        }
    }
}
