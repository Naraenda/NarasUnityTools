using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Nara.Editor
{
    public static class UI {
        public static bool BeginFoldout(bool state, string name) {
            #if UNITY_2018
                return EditorGUILayout.Foldout(state, name, UIStyles.Foldout);
            #endif

            #if UNITY_2019
                return EditorGUILayout.BeginFoldoutHeaderGroup(state, name);
            #endif
        }

        public static void EndFoldout() {
            #if UNITY_2019
                EditorGUILayout.EndFoldoutHeaderGroup();
            #endif
        }

        public static void Space(float spacing) {
            GUILayout.Space(spacing);
        }

        public static void Foldout(ref bool state, string name, Action foldout) {
            state = UI.BeginFoldout(state, name);
            if (state) foldout();
            UI.EndFoldout();
        }

        public static void Horizontal(Action horizontal) {
            GUILayout.BeginHorizontal();
            horizontal();
            GUILayout.EndHorizontal();
        }

        public static void Centered(Action centered) {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                centered();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
        }

        public static void Divider() {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);

            rect.height = 1;
            rect = EditorGUI.IndentedRect(rect);

            EditorGUI.DrawRect(rect, new Color (0.5f,0.5f,0.5f, 1));
        }

        public static void Divider(float spacing) {
            UI.Space(spacing);
            Divider();
            UI.Space(spacing);
        }
    }

    public static class UIStyles {       
        public static GUIStyle Foldout {
            get {
                var style = EditorStyles.foldout;
                style.fontStyle = FontStyle.Bold;
                return style;
            }
        }
    }
}