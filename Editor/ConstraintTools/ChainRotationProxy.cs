﻿#if UNITY_EDITOR

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEditor;

using Nara;
using Nara.Editor;
using Nara.Editor.ConstraintTools;

[ExecuteInEditMode, CanEditMultipleObjects, System.Serializable]
public class ChainRotationProxy : EditorWindow {
    static EditorWindow _window;
    Texture _logo;
    Vector2 _scrollPosition;

    // Constrained objects options
    int _constrainedObjCount = 1;
    List<GameObject> _constrainedObj = new List<GameObject>();
    bool _showConstrainedObj = true;

    // Constraint source options
    GameObject _constraintSource = null;
    bool _modifySource = true;
    string _rootName = "";

    // Generation options
    bool _showManualButtons = false;

    [MenuItem("Window/Nara/Chain Rotation Proxy", false, 900000)]
    public static void ShowWindow() {
        if(!_window) {
            _window  = EditorWindow.GetWindow(typeof(ChainRotationProxy));
            _window.autoRepaintOnSceneChange = true;
        }
        _window.titleContent = new GUIContent("Chain Rotation Proxy");
        _window.Show();
    }

    void OnGUI() {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        // [GUI] Constraint source options
        _constraintSource = (GameObject)EditorGUILayout.ObjectField($"Constraint source root", _constraintSource, typeof(GameObject), true);

        _modifySource = EditorGUILayout.Toggle("Modify source constraint", _modifySource);

        if (!_constraintSource && !_modifySource)
             EditorGUILayout.HelpBox("A constraint source root is needed to generate constraints. Or use modify source constraint to generate a new source object.", MessageType.Warning);

        if (!_constraintSource && _modifySource)
             EditorGUILayout.HelpBox("A new Gameobject as constraint source will be generated under the parent of the first element of the constraint bones. Children of source object might have their positions altered.", MessageType.Info);

        _rootName = _constraintSource ? Regex.Match(_constraintSource.name, @"^(.*?)_?\d*$").Groups[1].Value : "";

        UI.Space(16);

        // [GUI] Constrained objects count
        _constrainedObjCount = EditorGUILayout.IntField("Constrained objects count", _constrainedObjCount);
        if (_constrainedObjCount <= 0) {
            _constrainedObj.Clear();
            _constrainedObjCount = 1;
        }
        
        // [GUI] Constrained objects manual selection
        UI.Foldout(ref _showConstrainedObj, "Constrained objects", () => {
            EditorGUI.indentLevel++;

            List<GameObject> constraintBonesTemp = new List<GameObject>(_constrainedObjCount);
            for (int i = 0; i < _constrainedObjCount; i++)
            {
                GameObject cBone = i < _constrainedObj.Count ? _constrainedObj[i] : null;

                cBone = (GameObject)EditorGUILayout.ObjectField( $"Element {i}", cBone, typeof(GameObject), true);

                constraintBonesTemp.Add(cBone);
            }
            _constrainedObj = constraintBonesTemp.Where(x => x != null).ToList();

            EditorGUI.indentLevel--;
        });

        // [GUI] Constrained objects automatic selection
        UI.Horizontal(() =>
        {
            if(GUILayout.Button("From selection")) {
                _constrainedObj = Selection.gameObjects
                    .Where(x => x != _constraintSource)
                    .Reverse()
                    .ToList();
                _constrainedObjCount = Math.Max(_constrainedObj.Count, _constrainedObjCount);
            }
            if(GUILayout.Button("Select similar")) {
                var parents = _constrainedObj.Select(x => x.Parent()).Distinct();
                var names = _constrainedObj.Select(x => Regex.Replace(x.name, @"(_?\d+)*$", "")).Distinct();

                _constrainedObj = parents
                    .SelectMany(x => x.Children())
                    .Where(x => names.Any(name => x.name.StartsWith(name)))
                    .Where(x => x != _constraintSource)
                    .ToList();
                _constrainedObjCount = _constrainedObj.Count;
            }
        });

        UI.Space(24);

        // [GUI] Generator buttons
        var canMakeConstraints = _constraintSource != null;
        var canGenerateSource = _modifySource;
        var canMagic = canMakeConstraints || canGenerateSource;

        // [GUI] Best button <3
        using(new EditorGUI.DisabledScope(!canMagic))
            if (GUILayout.Button("✧･ﾟ: *✧･ﾟ:*    Setup constraints    *:･ﾟ✧*:･ﾟ✧" ,GUILayout.Height(45))) {
                if(_modifySource) {
                    if (_constraintSource == null) {
                        _constraintSource = ConstraintUtils.CreateSource(_constrainedObj);
                        _rootName = $"{_constraintSource.name}_source";
                        _constraintSource.name = $"{_rootName}_0";
                    }

                    Debug.Log("Extending chain and setting heights");
                    ConstraintUtils.ExtendChain(_constraintSource, _constrainedObj, _rootName);

                    var position = _constraintSource.transform.position;
                    ForEachParentAndSibling(ConstraintUtils.SetSourceChainY);
                    _constraintSource.transform.position = position;
                }
                
                Debug.Log("Creating constraints.");
                ForEachParentAndSibling(ConstraintUtils.CreateConstraints);
                ForEachParentAndSibling(ConstraintUtils.ActivateConstraints);
                Debug.Log("Done!");

                Selection.activeGameObject = _constraintSource;
            }

        UI.Space(24);

        // [GUI] Manual generation buttons
        UI.Foldout(ref _showManualButtons, "Manual setup", () => {
            EditorGUILayout.HelpBox("You probably don't need  any this, unless you really know what you're doing.", MessageType.Info);

            using(new EditorGUI.DisabledScope(canMakeConstraints))
                if (GUILayout.Button("Generate source chain")) {
                        _constraintSource = ConstraintUtils.CreateSource(_constrainedObj);
                        _rootName = $"{_constraintSource.name}_source";
                        _constraintSource.name = $"{_rootName}_0";

                        ConstraintUtils.ExtendChain(_constraintSource, _constrainedObj, _rootName);
                        ForEachParentAndSibling(ConstraintUtils.SetSourceChainY);
                }

            using(new EditorGUI.DisabledScope(!canMakeConstraints))
                if (GUILayout.Button("Add constraint components"))
                    ForEachParentAndSibling(ConstraintUtils.CreateConstraints);

            if (GUILayout.Button("Activate constraints"))
                ForEachParentAndSibling(ConstraintUtils.ActivateConstraints);

            if (GUILayout.Button("Remove constraints"))
                ForEachParentAndSibling(ConstraintUtils.RemoveConstraints);

            if (GUILayout.Button("Create rotation puppet"))
                _constrainedObj.ForEach(x => CreateClone(_constraintSource, x));
        });

        EditorGUILayout.EndScrollView();
    }

    void CreateClone(GameObject from, GameObject to) {
        ConstraintUtils.CreateConstraints(from, new GameObject[] { to });
        foreach (var to_child in to.Children()) {
            var res = from.Children().Where(x => x.name == to_child.name);
            var count = res.Count();
            if (count > 1)
                throw new Exception("Creating rotation clone failed: found duplicate names");

            if (count < 1)
                Debug.LogWarning($"Could not find bone named {to_child.name}");

            CreateClone(res.First(), to_child);
        }
    }

    void ForEachParentAndSibling(Action<GameObject, List<GameObject>> action) {
        var parent = _constraintSource;
        var siblings = _constrainedObj;
        var nextSiblings = new List<GameObject>();

        while (siblings.Count > 0) {
            nextSiblings.Clear();
            action(parent, siblings);

            parent = ConstraintUtils.FindNextInChain(parent, _rootName);

            if (parent == null)
                return;

            siblings = siblings.SelectMany(x => x.Children()).ToList();
        }
    }
}
#endif