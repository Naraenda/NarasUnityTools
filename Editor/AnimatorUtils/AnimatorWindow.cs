#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Linq;
using System;

using Nara;
using Nara.Editor;

[ExecuteInEditMode, CanEditMultipleObjects, System.Serializable]
public class AnimatorWindow : EditorWindow {
    static EditorWindow _window;
    Vector2 _scrollPosition = Vector2.zero;

    bool _showTransitions = true;
    bool _showStates = false;
    bool _showTimings = false;

    Dictionary<AnimatorStateTransition, List<int>> _selectedConditions = new Dictionary<AnimatorStateTransition, List<int>>();
    AnimatorStateTransition[] _selectedTransitions;
    (AnimatorStateTransition transition, string name)[] _selectedTransitionsNamed;
    AnimatorState[] _selectedStates;
    AnimatorController _animatorController;
    Dictionary<AnimatorState, Vector3> _stateNodes = new Dictionary<AnimatorState, Vector3>();

    // Mass condition editing parameters
    float _setAllThreshold = 0;
    AnimatorConditionMode _setAllMode = AnimatorConditionMode.Equals;
    string _setAllParameter = "";

    // Mass transition editing parameters
    bool  _setAllHasExitTime = false;
    float _setAllExitTime = 0;
    float _setAllDuration = 0;
    float _setAllOffset = 0;

    public enum SortMode { None, Alphabetical, Vertical, Horizontal }
    SortMode _sortMode = SortMode.None;

    [MenuItem("Window/Nara/Animator Tools", false, 900000)]
    public static void ShowWindow() {
        if(!_window) {
            _window  = EditorWindow.GetWindow(typeof(AnimatorWindow));
            _window.autoRepaintOnSceneChange = true;
        }
        _window.titleContent = new GUIContent("Animator Tools");
        _window.Show();
    }
    void OnGUI() {
        AnimatorController ac = _animatorController;
        if (ac == null) {
            return;
        }

        string[] availableParameters = _animatorController.parameters.Select(p => p.name).ToArray();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        EditorGUILayout.HelpBox("This is a work in progress!", MessageType.Info);

        UI.Foldout(ref _showStates, "States", () => {
            EditorGUI.indentLevel++;

            foreach (var state in _selectedStates)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(state.name);

                GUILayout.BeginVertical();
                if (GUILayout.Button("Select transitions from"))
                    Selection.objects = Selection.objects.Concat(state.transitions).ToArray();

                if (GUILayout.Button("Select transitions to"))
                    Selection.objects = Selection.objects.Concat(_stateTransitions.Where(t => t.Key.destinationState == state).Select(t => t.Key)).ToArray();

                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }
            
            EditorGUI.indentLevel--;
        });

        UI.Foldout(ref _showTransitions, "Transitions", () => {
            EditorGUI.indentLevel++;
            
            // Mass condition editor
            using (new EditorGUI.DisabledScope(_selectedConditions.All(c => c.Value.Count == 0))) {
                EditorGUILayout.BeginHorizontal();
                _setAllParameter = availableParameters.Length == 0 ? "" : availableParameters[
                    EditorGUILayout.Popup("Parameter", Math.Max(0, Array.IndexOf(availableParameters, _setAllParameter)), availableParameters)];
                if (GUILayout.Button("Set"))
                    SetParameter(_setAllParameter);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                _setAllMode = (AnimatorConditionMode)
                    EditorGUILayout.EnumPopup("Condition", _setAllMode);
                if (GUILayout.Button("Set"))
                    SetMode(_setAllMode);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                _setAllThreshold = 
                    EditorGUILayout.FloatField("Threshold", _setAllThreshold);
                if (GUILayout.Button("Set"))
                    SetThreshold(_setAllThreshold);
                if (GUILayout.Button("Auto"))
                    SetAutoThreshold();
                EditorGUILayout.EndHorizontal();
            }
            UI.Divider(5);

            // Mass transition editor
            EditorGUILayout.BeginHorizontal();
            _setAllHasExitTime = EditorGUILayout.Toggle("Has Exit Time", _setAllHasExitTime);
            GUILayout.Space(36);
            if (GUILayout.Button("Set"))
                ForEachTransition(t => t.hasExitTime = _setAllHasExitTime);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _setAllExitTime = EditorGUILayout.FloatField("Exit Time", _setAllExitTime);
            if (GUILayout.Button("Set"))
                ForEachTransition(t => t.exitTime = _setAllExitTime);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _setAllDuration = EditorGUILayout.FloatField("Duration", _setAllExitTime);
            if (GUILayout.Button("Set"))
                ForEachTransition(t => t.duration = _setAllDuration);
            EditorGUILayout.EndHorizontal(); 
            
            EditorGUILayout.BeginHorizontal();
            _setAllOffset = EditorGUILayout.FloatField("Offset", _setAllOffset);
            if (GUILayout.Button("Set"))
                ForEachTransition(t => t.offset = _setAllOffset);
            EditorGUILayout.EndHorizontal();

            UI.Divider(5);

            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Select all"))
                SelectAll();
            if(GUILayout.Button("Select none"))
                SelectNone();
            if(GUILayout.Button("Invert"))
                SelectInvert();
            EditorGUILayout.EndHorizontal();

            _sortMode = (SortMode)EditorGUILayout.EnumPopup("Sort by", _sortMode);
            switch (_sortMode)
            {
                case SortMode.Alphabetical:
                    _selectedTransitionsNamed = _selectedTransitionsNamed.OrderBy(_ => _.name).ToArray();
                    break;
                case SortMode.Horizontal:
                    _selectedTransitionsNamed = _selectedTransitionsNamed.OrderBy(t => {
                        var s = _stateNodes[_stateTransitions[t.transition]];
                        var d = _stateNodes[t.transition.destinationState];
                        var p = (s + d) / 2;
                        return (p.x, p.y, s.y);
                    }).ToArray();
                    break;
                case SortMode.Vertical:
                    _selectedTransitionsNamed = _selectedTransitionsNamed.OrderBy(t => {
                        var s = _stateNodes[_stateTransitions[t.transition]];
                        var d = _stateNodes[t.transition.destinationState];
                        var p = (s + d) / 2;
                        return (p.y, p.x, s.x);
                    }).ToArray();
                    break;
                default:
                    break;
            }
            UI.Divider(5);

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledGroupScope(!_showTimings))
                if(GUILayout.Button("Show Conditions"))
                    _showTimings = false;

            using (new EditorGUI.DisabledGroupScope(_showTimings))
                if (GUILayout.Button("Show Timings"))
                    _showTimings = true;
            EditorGUILayout.EndHorizontal();

            UI.Divider(5);

            EditorGUI.indentLevel++;
            for (int t_i = 0; t_i < _selectedTransitionsNamed.Length; t_i++)
            {
                var transition = _selectedTransitionsNamed[t_i].transition;

                if (!_selectedConditions.ContainsKey(transition))
                    _selectedConditions.Add(transition, new List<int>());

                string srcName = _anyStateTransitions.Contains(transition) ? "Any" : _stateTransitions[transition].name;
                string dstName = transition.destinationState?.name ?? "Exit";
                string name = transition.name == "" ? $"{srcName} -> {dstName}": transition.name;

                EditorGUILayout.BeginHorizontal();
                    
                EditorGUILayout.LabelField($"{name}", GUILayout.Width(170));

                if (!_showTimings) {
                    EditorGUILayout.BeginVertical();
                    var oldSelectedConditions = _selectedConditions[transition].ToList();
                    _selectedConditions[transition].Clear();

                    var conditions = new List<AnimatorCondition>();
                    for (int tc_i = 0, tc_j = 0; tc_i < transition.conditions.Length; tc_i++) {
                        var condition = transition.conditions[tc_i];

                        var parameter = _animatorController.parameters.Single(p => p.name == condition.parameter);

                        EditorGUILayout.BeginHorizontal();
                        EditorGUIUtility.labelWidth = 0f;

                        if (GUILayout.Toggle(oldSelectedConditions.Contains(tc_i), GUIContent.none))
                            _selectedConditions[transition].Add(tc_j);

                        GUILayout.Space(-30);

                        condition.parameter = availableParameters[
                            EditorGUILayout.Popup(GUIContent.none, Array.IndexOf(availableParameters, condition.parameter), availableParameters.Select(t => new GUIContent(t)).ToArray(), GUILayout.MinWidth(105))
                            ];

                        GUILayout.Space(-30);

                        condition.mode = (AnimatorConditionMode)
                            EditorGUILayout.EnumPopup(GUIContent.none, condition.mode, (m) => ParameterFilter(parameter.type, (AnimatorConditionMode)m), true, GUILayout.MaxWidth(105));

                        GUILayout.Space(-30);

                        using (new EditorGUI.DisabledScope(parameter.type == AnimatorControllerParameterType.Bool))
                            condition.threshold = 
                                EditorGUILayout.FloatField(GUIContent.none, condition.threshold, GUILayout.MaxWidth(75));

                        if (!GUILayout.Button(" - ")) {
                            conditions.Add(condition);
                            tc_j++;
                        }

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                    }

                    transition.conditions = conditions.ToArray();
                    
                    if (GUILayout.Button(" + ")) {
                        transition.AddCondition(AnimatorConditionMode.Equals, 0, availableParameters[0]);
                    }

                    EditorGUILayout.EndVertical();
                } else {
                    EditorGUILayout.BeginVertical();

                    transition.hasExitTime = 
                        EditorGUILayout.Toggle("Has Exit Time", transition.hasExitTime);

                    using (new EditorGUI.DisabledScope(!transition.hasExitTime))
                        transition.exitTime = 
                            EditorGUILayout.FloatField("Exit Time", transition.exitTime);

                    transition.hasFixedDuration = 
                        EditorGUILayout.Toggle("Has Fixed Duration", transition.hasFixedDuration);
                    transition.duration = 
                        EditorGUILayout.FloatField($"Duration ({(transition.hasFixedDuration ? 's' : '%')})", transition.duration);

                    transition.offset =
                        EditorGUILayout.FloatField("Offset", transition.offset);

                    transition.interruptionSource = (TransitionInterruptionSource)
                        EditorGUILayout.EnumPopup("Interruption Source", transition.interruptionSource);

                    using (new EditorGUI.DisabledScope(transition.interruptionSource == TransitionInterruptionSource.None))
                        transition.orderedInterruption =
                            EditorGUILayout.Toggle("Ordered interruption", transition.orderedInterruption);

                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();

                if (t_i < _selectedTransitions.Length - 1)
                    UI.Divider();
            }


            EditorGUI.indentLevel -= 2;
        });

        EditorGUILayout.EndScrollView();
    }

    void SelectAll() {
        for (int t_i = 0; t_i < _selectedTransitionsNamed.Length; t_i++) {
            var transition = _selectedTransitionsNamed[t_i].transition;

            if (_selectedConditions.ContainsKey(transition)) {
                _selectedConditions[transition].Clear();
            } else {
                _selectedConditions.Add(transition, new List<int>());
            }

            _selectedConditions[transition].AddRange(Enumerable.Range(0, transition.conditions.Length));
        }
    }

    void SelectNone() {
        for (int t_i = 0; t_i < _selectedTransitionsNamed.Length; t_i++) {
            var transition = _selectedTransitionsNamed[t_i].transition;
            if (_selectedConditions.ContainsKey(transition)) {
                _selectedConditions.Clear();
            }
        }
    }

    void SelectInvert() {
        for (int t_i = 0; t_i < _selectedTransitionsNamed.Length; t_i++) {
            var transition = _selectedTransitionsNamed[t_i].transition;

            List<int> selected;

            if (_selectedConditions.ContainsKey(transition)) {
                selected = _selectedConditions[transition];
                _selectedConditions[transition] = new List<int>();
            } else {
                selected = new List<int>();
                _selectedConditions.Add(transition, new List<int>());
            }

            _selectedConditions[transition].AddRange(Enumerable.Range(0, transition.conditions.Length).Except(selected));
        }
    }

    void SetParameter(string p) {
        foreach (var transition in _selectedTransitions) {
            var conditions = transition.conditions.ToArray();
            foreach (var tc_i in _selectedConditions[transition]) {
                conditions[tc_i].parameter = p;
            }
            transition.conditions = conditions;
        }
    }
    void SetMode(AnimatorConditionMode m) {
        foreach (var transition in _selectedTransitions) {
            var conditions = transition.conditions.ToArray();
            foreach (var tc_i in _selectedConditions[transition]) {
                conditions[tc_i].mode = m;
            }
            transition.conditions = conditions;
        }
    }

    void SetThreshold(float t) {
        foreach (var transition in _selectedTransitions) {
            var conditions = transition.conditions.ToArray();
            foreach (var tc_i in _selectedConditions[transition]) {
                conditions[tc_i].threshold = t;
            }
            transition.conditions = conditions;
        }
    }

    void SetAutoThreshold() {
        int t = 0;
        foreach (var transition in _selectedTransitions) {
            var conditions = transition.conditions.ToArray();
            foreach (var tc_i in _selectedConditions[transition]) {
                var param = _animatorController.parameters.Single(p => p.name == transition.conditions[tc_i].parameter);
                if (param.type == AnimatorControllerParameterType.Int)
                    conditions[tc_i].threshold = t++;
            }
            transition.conditions = conditions;
        }
    }

    void ForEachTransition(Action<AnimatorStateTransition> update) {
        foreach (var transition in _selectedTransitions)
            update(transition);
    }

    List<AnimatorStateTransition> _anyStateTransitions = new List<AnimatorStateTransition>();
    List<AnimatorTransition> _entryStateTransitions = new List<AnimatorTransition>();
    Dictionary<AnimatorStateTransition, AnimatorState> _stateTransitions = new Dictionary<AnimatorStateTransition, AnimatorState>();
    void OnSelectionChange() {
        _animatorController = GetCurrentController() ?? _animatorController;

        if (_animatorController == null)
            return;
        _sortMode = SortMode.None;
        _anyStateTransitions.Clear();
        _entryStateTransitions.Clear();
        _stateTransitions.Clear();
        _stateNodes.Clear();

        foreach (var layer in _animatorController.layers) {
            _anyStateTransitions.AddRange(layer.stateMachine.anyStateTransitions);
            _entryStateTransitions.AddRange(layer.stateMachine.entryTransitions);

            /*
            foreach (var state in layer.stateMachine.states) {
                _stateNodes.Add(state.state, state.position);
                foreach (var transition in state.state.transitions) {
                    _stateTransitions.Add(transition, state.state);
                }
            } */

			foreach (var state in layer.stateMachine.AllStates()) {
                _stateNodes.Add(state.state, state.position);
                foreach (var transition in state.state.transitions) {
                    _stateTransitions.Add(transition, state.state);
                }
            }
        }

        _selectedTransitions = Selection.objects
            .Where(o => o.GetType() == typeof(AnimatorStateTransition))
            .Select(o => o as AnimatorStateTransition).ToArray();

        _selectedStates = Selection.objects
            .Where(o => o.GetType() == typeof(AnimatorState))
            .Select(o => o as AnimatorState).ToArray();

        _selectedTransitionsNamed = _selectedTransitions.Select(t => {
            string srcName = _anyStateTransitions.Contains(t) ? "Any" : _stateTransitions[t].name;
            string dstName = t.destinationState?.name ?? "Exit";
            string name = t.name == "" ? $"{srcName} -> {dstName}": t.name;
            return (transition: t, name: name);
        }).ToArray();

        Repaint();
    }
    static UnityEditor.Animations.AnimatorController GetCurrentController()
    {
        UnityEditor.Animations.AnimatorController controller = null;
        var tool = EditorWindow.focusedWindow;
        var toolType = tool.GetType();
        var controllerProperty = toolType.GetProperty("animatorController", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if(controllerProperty != null) {
            controller = controllerProperty.GetValue(tool, null) as UnityEditor.Animations.AnimatorController;
        } else {
            //Debug.Log("EditorWindow.focusedWindow " + tool + " does not contain animatorController", tool);
        }

        return controller;
    }

    static bool ParameterFilter(AnimatorControllerParameterType t, AnimatorConditionMode m) {

        var b = new AnimatorConditionMode[] {
            AnimatorConditionMode.If,
            AnimatorConditionMode.IfNot };
        var f = new AnimatorConditionMode[] {
            AnimatorConditionMode.Greater,
            AnimatorConditionMode.Less };
        var i = new AnimatorConditionMode[] {
            AnimatorConditionMode.Greater,
            AnimatorConditionMode.Less,
            AnimatorConditionMode.Equals,
            AnimatorConditionMode.NotEqual };


        if (t == AnimatorControllerParameterType.Bool)
            return b.Contains(m);
        else if (t == AnimatorControllerParameterType.Float)
            return f.Contains(m);
        else if (t == AnimatorControllerParameterType.Int)
            return i.Contains(m);
        return false;
    }
    private GUIStyle AreaStyleNoMargin { get { GUIStyle s = new GUIStyle(EditorStyles.textArea) { margin = new RectOffset(0, 0, 0, 0), }; return s; } }
}
#endif
