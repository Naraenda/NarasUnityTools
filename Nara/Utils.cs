using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Nara
{
    public static class Utils {
        public static IEnumerable<GameObject> Children(this GameObject parent)
            => parent.GetComponentsInChildren<Transform>().Select(x => x.gameObject).Where(x => x.Parent() == parent);

        public static GameObject Parent(this GameObject child)
            => child.transform.parent.gameObject;
    }

#if (UNITY_EDITOR)
    public static class UIUtils {
        public static bool BeginFoldoutHeaderGroup(bool state, string name) {
            #if UNITY_2018
                return EditorGUILayout.Foldout(state, name);
            #endif

            #if UNITY_2019
                return EditorGUILayout.BeginFoldoutHeaderGroup(state, name);
            #endif
        }

        public static void EndFoldoutHeaderGroup() {
            #if UNITY_2019
                EditorGUILayout.EndFoldoutHeaderGroup();
            #endif
        }

        public static void Space(float spacing) {
            #if UNITY_2018
                EditorGUILayout.Space();
            #endif
            #if UNITY_2019
                EditorGUILayout.Space(spacing);
            #endif
        }

    }
#endif
}