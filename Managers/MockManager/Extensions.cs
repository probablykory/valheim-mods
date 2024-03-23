using System.Reflection;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MockManager
{
    /// <summary>
    ///     Extends GameObject with a shortcut for the Unity bool operator override.
    /// </summary>
    public static class ExposedGameObjectExtension
    {
        /// <summary>
        ///     Facilitates use of null propagation operator for unity GameObjects by respecting op_equality.
        /// </summary>
        /// <param name="this"> this </param>
        /// <returns>Returns null when GameObject.op_equality returns false.</returns>
        public static GameObject OrNull(this GameObject @this)
        {
            return @this ? @this : null;
        }

        /// <summary>
        ///     Facilitates use of null propagation operator for unity MonBehaviours by respecting op_equality.
        /// </summary>
        /// <typeparam name="T">Any type that inherits MonoBehaviour</typeparam>
        /// <param name="this">this</param>
        /// <returns>Returns null when MonoBehaviours.op_equality returns false.</returns>
        public static T OrNull<T>(this T @this) where T : UnityEngine.Object
        {
            return (T)(@this ? @this : null);
        }

        /// <summary>
        ///     Returns the component of Type type. If one doesn't already exist on the GameObject it will be added.
        /// </summary>
        /// <remarks>Source: https://wiki.unity3d.com/index.php/GetOrAddComponent</remarks>
        /// <typeparam name="T">The type of Component to return.</typeparam>
        /// <param name="gameObject">The GameObject this Component is attached to.</param>
        /// <returns>Component</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.TryGetComponent(out T component) ? component : gameObject.AddComponent<T>();
        }

        /// <summary>
        ///     Adds a new copy of the provided component to a gameObject
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="duplicate"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Component AddComponentCopy<T>(this GameObject gameObject, T duplicate) where T : Component
        {
            Component target = gameObject.AddComponent(duplicate.GetType());
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (PropertyInfo propertyInfo in duplicate.GetType().GetProperties(flags))
            {
                switch (propertyInfo.Name)
                {
                    // setting rayTracingMode prints a warning, because ray tracing is disabled
                    case "rayTracingMode":
                        continue;
                    // this is Component.name and is shared with the GameObject name. Copying a component should not change the GameObject name
                    case "name":
                        continue;
                    // this is Component.tag and sets the GameObject tag. Copying a component should not change the GameObject tag
                    case "tag":
                        continue;
                    // not allowed to access
                    case "mesh":
                        if (duplicate is MeshFilter)
                            continue;
                        break;
                    // not allowed to access
                    case "material":
                    case "materials":
                        if (duplicate is Renderer)
                            continue;
                        break;
                    // setting the bounds overrides the default bounding box and the renderer bounding volume will no longer be automatically calculated
                    case "bounds":
                        if (duplicate is Renderer)
                            continue;
                        break;
                }

                if (propertyInfo.CanWrite && propertyInfo.GetMethod != null)
                {
                    propertyInfo.SetValue(target, propertyInfo.GetValue(duplicate));
                }
            }

            foreach (FieldInfo fieldInfo in duplicate.GetType().GetFields(flags))
            {
                if (fieldInfo.Name == "rayTracingMode")
                {
                    continue;
                }

                fieldInfo.SetValue(target, fieldInfo.GetValue(duplicate));
            }

            return target;
        }

        /// <summary>
        ///     Check if GameObject has any of the specified components.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="components"></param>
        /// <returns></returns>
        public static bool HasAnyComponent(this GameObject gameObject, params Type[] components)
        {
            foreach (var compo in components)
            {
                if (gameObject.GetComponent(compo))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Check if GameObject has any of the specified components.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="componentNames"></param>
        /// <returns></returns>
        public static bool HasAnyComponent(this GameObject gameObject, params string[] componentNames)
        {
            foreach (var name in componentNames)
            {
                if (gameObject.GetComponent(name))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Check if GameObject has all of the specified components.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="componentNames"></param>
        /// <returns></returns>
        public static bool HasAllComponents(this GameObject gameObject, params string[] componentNames)
        {
            foreach (var name in componentNames)
            {
                if (!gameObject.GetComponent(name))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Check if GameObject has all of the specified components.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="components"></param>
        /// <returns></returns>
        public static bool HasAllComponents(this GameObject gameObject, params Type[] components)
        {
            foreach (var compo in components)
            {
                if (!gameObject.GetComponent(compo))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Check if GameObject or any of it's children
        ///     have any of the specified components.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="includeInactive"></param>
        /// <param name="components"></param>
        /// <returns></returns>
        public static bool HasAnyComponentInChildren(
            this GameObject gameObject,
            bool includeInactive = false,
            params Type[] components
        )
        {
            foreach (var compo in components)
            {
                if (gameObject.GetComponentInChildren(compo, includeInactive))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Extension method to find nested children by name using either
        ///     a breadth-first or depth-first search. Default is breadth-first.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="childName">Name of the child object to search for.</param>
        /// <param name="searchType">Whether to preform a breadth first or depth first search. Default is breadth first.</param>
        public static Transform FindDeepChild(
            this GameObject gameObject,
            string childName,
            global::Utils.IterativeSearchType searchType = global::Utils.IterativeSearchType.BreadthFirst
        )
        {
            return gameObject.transform.FindDeepChild(childName, searchType);
        }

        /// <summary>
        ///     Extension method to find nested children by an ordered list of names using either
        ///     a breadth-first or depth-first search. Default is breadth-first.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="childNames">Names in order of the child object to search for.</param>
        /// <param name="searchType">Whether to preform a breadth first or depth first search. Default is breadth first.</param>
        public static Transform FindDeepChild(
            this GameObject gameObject,
            IEnumerable<string> childNames,
            global::Utils.IterativeSearchType searchType = global::Utils.IterativeSearchType.BreadthFirst
        )
        {
            var child = gameObject.transform;

            foreach (string childName in childNames)
            {
                child = child.FindDeepChild(childName, searchType);

                if (!child)
                {
                    return null;
                }
            }

            return child;
        }
    }

    /// <summary>
    ///     Extends prefab GameObjects with functionality related to the mocking system.
    /// </summary>
    public static class PrefabExtension
    {
        /// <summary>
        ///     Will attempt to fix every field that are mocks gameObjects / Components from the given object.
        /// </summary>
        /// <param name="objectToFix"></param>
        public static void FixReferences(this object objectToFix)
        {
            MockManager.FixReferences(objectToFix, 0);
        }

        /// <summary>
        ///     Resolves all references for mocks in this GameObject's components recursively
        /// </summary>
        /// <param name="gameObject"></param>
        public static void FixReferences(this GameObject gameObject)
        {
            gameObject.FixReferences(false);
        }

        /// <summary>
        ///     Resolves all references for mocks of components in this collection of GameObjects recursively
        /// </summary>
        /// <param name="gameObjects"></param>
        public static void FixReferences(this ICollection<GameObject> gameObjects, bool recursive = false)
        {
            foreach (var go in gameObjects)
            {
                go.FixReferences(recursive);
            }
        }

        /// <summary>
        ///     Resolves all references for mocks in this GameObject recursively.
        ///     Can additionally traverse the transforms hierarchy to fix child GameObjects recursively.
        /// </summary>
        /// <param name="gameObject">This GameObject</param>
        /// <param name="recursive">Traverse all child transforms</param>
        public static void FixReferences(this GameObject gameObject, bool recursive)
        {
            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (!(component is Transform))
                {
                    MockManager.FixReferences(component, 0);
                }
            }

            if (!recursive)
            {
                return;
            }

            List<Tuple<Transform, GameObject>> mockChildren = new List<Tuple<Transform, GameObject>>();

            foreach (Transform child in gameObject.transform)
            {
                var realPrefab = MockManager.GetRealPrefabFromMock<GameObject>(child.gameObject);

                if (realPrefab)
                {
                    mockChildren.Add(new Tuple<Transform, GameObject>(child, realPrefab));
                }
                else
                {
                    child.gameObject.FixReferences(true);
                }
            }

            // mock GameObjects have to be replaced in a second loop to avoid modifying the transform hierarchy while iterating over it
            foreach (var mockChild in mockChildren)
            {
                MockManager.ReplaceMockGameObject(mockChild.Item1, mockChild.Item2, gameObject);
            }
        }

        /// <summary>
        ///     Clones all fields from this GameObject to objectToClone.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="objectToClone"></param>
        public static void CloneFields(this GameObject gameObject, GameObject objectToClone)
        {
            const BindingFlags flags = ReflectionHelper.AllBindingFlags;

            var fieldValues = new Dictionary<FieldInfo, object>();
            var origComponents = objectToClone.GetComponentsInChildren<Component>();
            foreach (var origComponent in origComponents)
            {
                foreach (var fieldInfo in origComponent.GetType().GetFields(flags))
                {
                    if (!fieldInfo.IsLiteral && !fieldInfo.IsInitOnly)
                        fieldValues.Add(fieldInfo, fieldInfo.GetValue(origComponent));
                }

                if (!gameObject.GetComponent(origComponent.GetType()))
                {
                    gameObject.AddComponent(origComponent.GetType());
                }
            }

            var clonedComponents = gameObject.GetComponentsInChildren<Component>();
            foreach (var clonedComponent in clonedComponents)
            {
                foreach (var fieldInfo in clonedComponent.GetType().GetFields(flags))
                {
                    if (fieldValues.TryGetValue(fieldInfo, out var fieldValue))
                    {
                        fieldInfo.SetValue(clonedComponent, fieldValue);
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Convenience methods for Transforms
    /// </summary>
    public static class TransformExtensions
    {
        /// <summary>
        ///     Extension method to find nested children by name using either
        ///     a breadth-first or depth-first search. Default is breadth-first.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="childName">Name of the child object to search for.</param>
        /// <param name="searchType">Whether to preform a breadth first or depth first search. Default is breadth first.</param>
        /// <returns></returns>
        public static Transform FindDeepChild(
            this Transform transform,
            string childName,
            global::Utils.IterativeSearchType searchType = global::Utils.IterativeSearchType.BreadthFirst
        )
        {
            return global::Utils.FindChild(transform, childName, searchType);
        }
    }

    /// <summary>
    ///     Helper methods for strings
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        ///     Returns true if the string contains any of the substrings.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="substrings"></param>
        /// <returns></returns>
        public static bool ContainsAny(this string str, params string[] substrings)
        {
            foreach (var substring in substrings)
            {
                if (str.Contains(substring))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Returns true if the string ends with any one of the suffixes.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="suffixes"></param>
        /// <returns></returns>
        public static bool EndsWithAny(this string str, params string[] suffixes)
        {
            foreach (var substring in suffixes)
            {
                if (str.EndsWith(substring))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Returns true if the string starts with any one of the prefixes.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="prefixes"></param>
        /// <returns></returns>
        public static bool StartsWithAny(this string str, params string[] prefixes)
        {
            foreach (var substring in prefixes)
            {
                if (str.StartsWith(substring))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     If the string ends with the suffix then return a copy of the string
        ///     with the suffix stripped, otherwise return the original string.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static string RemoveSuffix(this string s, string suffix)
        {
            if (s.EndsWith(suffix))
            {
                return s.Substring(0, s.Length - suffix.Length);
            }

            return s;
        }

        /// <summary>
        ///     If the string starts with the prefix then return a copy of the string
        ///     with the prefix stripped, otherwise return the original string.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static string RemovePrefix(this string s, string prefix)
        {
            if (s.StartsWith(prefix))
            {
                return s.Substring(prefix.Length, s.Length - prefix.Length);
            }
            return s;
        }

        /// <summary>
        ///     Returns a copy of the string with the first character capitalized
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string CapitalizeFirstLetter(this string s)
        {
            if (s.Length == 0)
                return s;
            else if (s.Length == 1)
                return $"{char.ToUpper(s[0])}";
            else
                return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
