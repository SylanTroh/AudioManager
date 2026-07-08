using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sylan.AudioManager.EditorUtilities
{
    public static class SerializedPropertyUtils
    {
        /// <summary>
        /// Find exactly one object of Type T in the scene hierarchy.
        /// </summary>
        /// <typeparam name="T">Type of Object to get</typeparam>
        /// <param name="obj">Local variable to contain the object</param>
        /// <returns>True if successful, false if unsuccessful</returns>
        public static bool TryFindObject<T>(out T obj) where T : Component
        {
            T[] objects = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (objects.Length == 0)
            {
                Debug.Log("[EditorUtilities] No Objects of type " + typeof(T).ToString());
                obj = null;
                return true;
            }
            if (objects.Length > 1)
            {
                Debug.LogError("[EditorUtilities] More than one object of type " + typeof(T).ToString());
                obj = null;
                return false;
            }
            obj = objects[0];
            return true;
        }

        public static T[] FindAllObjects<T>() where T : Component
        {
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        }

        /// <summary>
        /// Set Serialized Property of Type T. Property must not be an array.
        /// </summary>
        /// <typeparam name="T">Type of Object to set</typeparam>
        /// <param name="serializedObject">Object with the property, can be <see langword="null"/>.</param>
        /// <param name="propertyName">Name of Serialized Property</param>
        public static void PopulateSerializedProperty<T>(SerializedObject serializedObject, string propertyName) where T : Component
        {
            if (serializedObject == null) return;

            // Get one matching components in the scene
            TryFindObject(out T obj); // May be null.
            serializedObject.FindProperty(propertyName).objectReferenceValue = obj;

            // Apply the changes to the component
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Set Serialized Property of Type T. Property must be an array, and will be filled with all the objects found.
        /// </summary>
        /// <typeparam name="T">Type of Object to set</typeparam>
        /// <param name="serializedObject">Object with the property, can be <see langword="null"/>.</param>
        /// <param name="propertyName">Name of Serialized Property</param>
        public static void PopulateSerializedArray<T>(SerializedObject serializedObject, string propertyName) where T : Component
        {
            if (serializedObject == null) return;

            SetArrayProperty(
                serializedObject.FindProperty(propertyName),
                FindAllObjects<T>(),
                (p, v) => p.objectReferenceValue = v);

            // Apply the changes to the component
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// <para>Must call <see cref="SerializedObject.ApplyModifiedProperties()"/> afterwards.</para>
        /// </summary>
        /// <typeparam name="T">Any type.</typeparam>
        /// <param name="property">An array property.</param>
        /// <param name="newValues">The values to populate the array with.</param>
        /// <param name="setValue">Delegate assigning one element (property) in the array its value.</param>
        public static void SetArrayProperty<T>(SerializedProperty property, ICollection<T> newValues, System.Action<SerializedProperty, T> setValue)
        {
            property.ClearArray();
            property.arraySize = newValues.Count;
            int i = 0;
            foreach (T value in newValues)
            {
                setValue(property.GetArrayElementAtIndex(i++), value);
            }
        }

        /// <summary>
        /// Find an object of Type T in the scene hierarchy as a SerializedObject.
        /// </summary>
        /// <typeparam name="T">Type of Object to get</typeparam>
        /// <param name="obj">Local variable to contain the object</param>
        /// <param name="serializedObject">Local variable to contain the object</param>
        /// <returns>True if successful, false if unsuccessful</returns>
        public static bool TryFindSerializedObject<T>(out T obj, out SerializedObject serializedObject) where T : Component
        {
            serializedObject = null;
            if (!TryFindObject(out obj)) return false;
            if (obj != null) serializedObject = new SerializedObject(obj);
            return true;
        }

        public static void SetLayerAndApply(GameObject go, int layer) => SetLayerAndApply(new SerializedObject(go), layer);
        public static void SetLayerAndApply(GameObject[] gos, int layer) => SetLayerAndApply(new SerializedObject(gos), layer);
        private static void SetLayerAndApply(SerializedObject gameObjectSo, int layer)
        {
            gameObjectSo.FindProperty("m_Layer").intValue = layer;
            gameObjectSo.ApplyModifiedProperties();
        }

        public static void DrawLayerField(SerializedProperty prop, GUIContent label)
        {
            Rect rect = EditorGUILayout.GetControlRect(hasLabel: true);
            EditorGUI.BeginProperty(rect, label: null, prop); // LayerField draws the label.
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
            int newLayer = EditorGUI.LayerField(rect, label, prop.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                prop.intValue = newLayer;
            }
            EditorGUI.EndProperty();
        }
    }
}
