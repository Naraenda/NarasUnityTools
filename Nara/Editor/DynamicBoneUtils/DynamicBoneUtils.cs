using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

public class DynamicBoneUtils
{
    [MenuItem("GameObject/DynamicBone/Set root", false, 0)]
    static void SetRootsFromSelection(MenuCommand menuCommand) {
        var sel = Selection.gameObjects;
        foreach (var go in sel) {
            var db = go?.GetComponent<DynamicBone>();

            if (go == null || db == null) continue;

            db.m_Root = go.transform;
        }
    }

    [MenuItem("GameObject/DynamicBone/Filter selection", false, 0)]
    static void FilterOnlyDynamicBone(MenuCommand menuCommand) {
        Selection.objects = Selection
            .GetFiltered<DynamicBone>(SelectionMode.Unfiltered)
            .Select(x => x.gameObject)
            .ToArray();
    }

    [MenuItem("GameObject/DynamicBone/Filter selection (with children)", false, 0)]
    static void FilterOnlyDynamicBoneAndChildren(MenuCommand menuCommand) {
        Selection.objects = Selection.gameObjects
            .SelectMany(x => x.GetComponentsInChildren<DynamicBone>())
            .Select(x => x.gameObject)
            .ToArray();
    }
}
