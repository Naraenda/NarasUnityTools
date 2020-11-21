using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEditor;

using Nara;
using Nara.DBC;

[ExecuteInEditMode, CanEditMultipleObjects, System.Serializable]
public class DBCWindow : EditorWindow {
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


    [MenuItem("Nara/Dynamic Bone Constraints", false, 0)]
    public static void ShowWindow() {
        if(!_window) {
            _window  = EditorWindow.GetWindow(typeof(DBCWindow));
            _window.autoRepaintOnSceneChange = true;
        }
        _window.titleContent = new GUIContent("Dynamic Bone Constraints");
        _window.Show();
    }

    void OnGUI() {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        // [GUI] Logo
        if (!_logo)
            _logo = (Texture)EditorGUIUtility.Load("Assets/Nara/DynBoneConstraints/logo.png");

        GUILayout.Label(_logo);
        UIUtils.Space(16);

        // [GUI] Constraint source options
        _constraintSource = (GameObject)EditorGUILayout.ObjectField($"Constraint source root", _constraintSource, typeof(GameObject), true);

        _modifySource = EditorGUILayout.Toggle("Modify source constraint", _modifySource);

        if (!_constraintSource && !_modifySource)
             EditorGUILayout.HelpBox("A constraint source root is needed to generate constraints. Or use modify source constraint to generate a new source object.", MessageType.Warning);

        if (!_constraintSource && _modifySource)
             EditorGUILayout.HelpBox("A new Gameobject as constraint source will be generated under the parent of the first element of the constraint bones. Children of source object might have their positions altered.", MessageType.Info);

        _rootName = _constraintSource ? Regex.Match(_constraintSource.name, @"^(.*?)_?\d*$").Groups[1].Value : "";

        UIUtils.Space(16);

        // [GUI] Constrained objects count
        _constrainedObjCount = EditorGUILayout.IntField("Constrained objects count", _constrainedObjCount);
        if (_constrainedObjCount <= 0) {
            _constrainedObj.Clear();
            _constrainedObjCount = 1;
        }
        
        // [GUI] Constrained objects manual selection
        _showConstrainedObj = UIUtils.BeginFoldoutHeaderGroup(_showConstrainedObj, "Constrained objects");
        if (_showConstrainedObj) {
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
        }
        UIUtils.EndFoldoutHeaderGroup();

        // [GUI] Constrained objects automatic selection
        EditorGUILayout.BeginHorizontal();
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
        }
        EditorGUILayout.EndHorizontal();

        UIUtils.Space(24);

        // [GUI] Generator buttons
        var canMakeConstraints = _constraintSource != null;
        var canGenerateSource = _modifySource;
        var canMagic = canMakeConstraints || canGenerateSource;

        // [GUI] Best button <3
        using(new EditorGUI.DisabledScope(!canMagic))
            if (GUILayout.Button("✧･ﾟ: *✧･ﾟ:*    Setup constraints    *:･ﾟ✧*:･ﾟ✧" ,GUILayout.Height(45))) {
                if(_modifySource) {
                    if (_constraintSource == null) {
                        _constraintSource = DBCUtils.CreateSource(_constrainedObj);
                        _rootName = $"{_constraintSource.name}_source";
                        _constraintSource.name = $"{_rootName}_0";
                    }

                    Debug.Log("Extending chain and setting heights");
                    DBCUtils.ExtendChain(_constraintSource, _constrainedObj, _rootName);

                    var position = _constraintSource.transform.position;
                    ForEachParentAndSibling(DBCUtils.SetSourceChainY);
                    _constraintSource.transform.position = position;
                }
                
                Debug.Log("Creating constraints.");
                ForEachParentAndSibling(DBCUtils.CreateConstraints);
                ForEachParentAndSibling(DBCUtils.ActivateConstraints);
                Debug.Log("Done!");

                Selection.activeGameObject = _constraintSource;
            }

        UIUtils.Space(24);

        // [GUI] Manual generation buttons
        _showManualButtons = UIUtils.BeginFoldoutHeaderGroup(_showManualButtons, "Manual setup");
        if (_showManualButtons) {
            EditorGUILayout.HelpBox("You probably don't need  any this, unless you really know what you're doing.", MessageType.Info);
            EditorGUI.indentLevel++;

            using(new EditorGUI.DisabledScope(canMakeConstraints))
                if (GUILayout.Button("Generate source chain")) {
                        _constraintSource = DBCUtils.CreateSource(_constrainedObj);
                        _rootName = $"{_constraintSource.name}_source";
                        _constraintSource.name = $"{_rootName}_0";

                        DBCUtils.ExtendChain(_constraintSource, _constrainedObj, _rootName);
                        ForEachParentAndSibling(DBCUtils.SetSourceChainY);
                }

            using(new EditorGUI.DisabledScope(!canMakeConstraints))
                if (GUILayout.Button("Add constraint components"))
                    ForEachParentAndSibling(DBCUtils.CreateConstraints);

            if (GUILayout.Button("Activate constraints"))
                ForEachParentAndSibling(DBCUtils.ActivateConstraints);

            if (GUILayout.Button("Remove constraints"))
                ForEachParentAndSibling(DBCUtils.RemoveConstraints);
            
            EditorGUI.indentLevel--;
        }
        UIUtils.EndFoldoutHeaderGroup();

        EditorGUILayout.EndScrollView();
    }

    void ForEachParentAndSibling(Action<GameObject, List<GameObject>> action) {
        var parent = _constraintSource;
        var siblings = _constrainedObj;
        var nextSiblings = new List<GameObject>();

        while (siblings.Count > 0) {
            nextSiblings.Clear();
            action(parent, siblings);

            parent = DBCUtils.FindNextInChain(parent, _rootName);

            if (parent == null)
                return;

            siblings = siblings.SelectMany(x => x.Children()).ToList();
        }
    }
}
