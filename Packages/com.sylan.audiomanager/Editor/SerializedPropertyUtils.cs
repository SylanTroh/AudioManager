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
        public static bool GetObject<T>(out T obj) where T : MonoBehaviour
        {
            T[] objects = UnityEngine.Object.FindObjectsOfType<T>();

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
        public static bool GetObjects<T>(out T[] obj) where T : MonoBehaviour
        {
            T[] objects = UnityEngine.Object.FindObjectsOfType<T>();
            obj = objects;
            return true;
        }
        /// <summary>
        /// Set Serialized Property of Type T. Property must not be an array.
        /// </summary>
        /// <typeparam name="T">Type of Object to set</typeparam>
        /// <param name="serializedObject">Object with the property</param>
        /// <param name="propertyName">Name of Serialized Property</param>
        public static void PopulateSerializedProperty<T>(SerializedObject serializedObject, string propertyName) where T : MonoBehaviour
        {
            if (serializedObject == null) return;
            SerializedProperty property;
            property = serializedObject.FindProperty(propertyName);

            // Get one matching components in the scene
            GetObject<T>(out T obj);
            property.objectReferenceValue = obj;

            // Apply the changes to the component
            serializedObject.ApplyModifiedProperties();
        }
        /// <summary>
        /// Set Serialized Property of Type T. Property must be an array, and will be filled with all the objects found.
        /// </summary>
        /// <typeparam name="T">Type of Object to set</typeparam>
        /// <param name="serializedObject">Object with the property</param>
        /// <param name="propertyName">Name of Serialized Property</param>
        public static void PopulateSerializedArray<T>(SerializedObject serializedObject, string propertyName) where T : MonoBehaviour
        {
            if (serializedObject == null) return;
            SerializedProperty arrayProperty;
            arrayProperty = serializedObject.FindProperty(propertyName);

            // Get all the matching components in the scene
            GetObjects<T>(out T[] objects);

            // Assign the serialized references to the array
            arrayProperty.ClearArray();
            arrayProperty.arraySize = objects.Length;
            for (int i = 0; i < objects.Length; i++)
            {
                arrayProperty.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
            }
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
        /// <param name="serializedObject">Local variable to contain the object</param>
        /// <returns>True if successful, false if unsuccessful</returns>
        public static bool GetSerializedObject<T>(out SerializedObject serializedObject) where T : MonoBehaviour
        {
            serializedObject = null;
            if (!GetObject(out T obj))
            {
                EditorApplication.isPlaying = false;
                return false;
            }
            if (obj != null) serializedObject = new SerializedObject(obj);
            return true;
        }
        /// <summary>
        /// Find objects of Type T in the scene hierarchy as an array of type SerializedObject.
        /// </summary>
        /// <typeparam name="T">Type of Object to get</typeparam>
        /// <param name="serializedObjects">Local variable to contain the objects</param>
        /// <returns>True if successful, false if unsuccessful</returns>
        public static bool GetSerializedObjects<T>(out SerializedObject[] serializedObjects) where T : MonoBehaviour
        {
            serializedObjects = null;
            if (!GetObjects(out T[] obj))
            {
                EditorApplication.isPlaying = false;
                return false;
            }
            serializedObjects = new SerializedObject[obj.Length];
            for (int i = 0; i < obj.Length; i++)
            {
                serializedObjects[i] = new SerializedObject(obj[i]);
            }
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
