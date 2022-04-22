using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.Dynamics;

public class PhysBoneMenuUtils
{
    [MenuItem("CONTEXT/VRCPhysBone/PhysBone/Set transform root to GameObject(s)", false, 0)]
    [MenuItem("GameObject/PhysBone/Set transform root to GameObject(s)", false, 0)]
    static void SetPBRoots(MenuCommand menuCommand) {
        var sel = Selection.gameObjects;
        foreach (var pb in sel.Select(obj => obj.GetComponent<VRCPhysBone>()).Where(c => c != null)) {
            pb.rootTransform = pb.transform;
        }
    }

    [MenuItem("GameObject/PhysBone/Set collisions to selected Collider(s)", false, 0)]
    static void SetPBFromSelectedColliders(MenuCommand menuCommand) {
        var sel = Selection.gameObjects;
        var colliders = sel.Select(obj => obj.GetComponent<VRCPhysBoneCollider>() as VRCPhysBoneColliderBase).Where(c => c != null);
        foreach (var pb in sel.Select(obj => obj.GetComponent<VRCPhysBone>()).Where(c => c != null)) {
            pb.colliders = colliders.ToList();
        }
    }

    [MenuItem("CONTEXT/VRCPhysBone/PhysBone/Set collisions to selected Collider(s)", false, 0)]
    static void SetPBFromSelectedCollidersContext(MenuCommand menuCommand) {
        var sel = Selection.gameObjects;
        var colliders = sel.Select(obj => obj.GetComponent<VRCPhysBoneCollider>() as VRCPhysBoneColliderBase).Where(c => c != null);
        var pb = (menuCommand.context as GameObject).GetComponent<VRCPhysBone>();
        pb.colliders = colliders.ToList();
    }

    [MenuItem("GameObject/PhysBone/Filter selection to PhysBone(s)", false, 0)]
    static void FilterPBFromSelection(MenuCommand menuCommand) {
        Selection.objects = Selection
            .GetFiltered<VRCPhysBone>(SelectionMode.Unfiltered)
            .Select(x => x.gameObject)
            .ToArray();
    }

    [MenuItem("GameObject/PhysBone/Filter selection to Collider(s)", false, 0)]
    static void FilterPBCFromSelection(MenuCommand menuCommand) {
        Selection.objects = Selection
            .GetFiltered<VRCPhysBoneCollider>(SelectionMode.Unfiltered)
            .Select(x => x.gameObject)
            .ToArray();
    }

    [MenuItem("GameObject/PhysBone/Select PhysBone(s) in children", false, 0)]
    static void SelectPBInChildren(MenuCommand menuCommand) {
        Selection.objects = Selection.gameObjects
            .SelectMany(x => x.GetComponentsInChildren<VRCPhysBone>())
            .Select(x => x.gameObject)
            .ToArray();
    }


    [MenuItem("GameObject/PhysBone/Select collider(s) in children", false, 0)]
    static void SelectPBCInChildren(MenuCommand menuCommand) {
        Selection.objects = Selection.gameObjects
            .SelectMany(x => x.GetComponentsInChildren<VRCPhysBoneCollider>())
            .Select(x => x.gameObject)
            .ToArray();
    }
}
