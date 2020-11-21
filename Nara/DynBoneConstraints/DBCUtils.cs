using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.Animations;

namespace Nara.DBC
{
    public static class DBCUtils
    {
        public static void SetSourceChainY(GameObject parent, IEnumerable<GameObject> siblings) {
            float avgY = siblings.Average(x => x.transform.position.y);
            var position = parent.transform.position;
            position.y = avgY;
            parent.transform.position = position;
        }

        public static void CreateConstraints(GameObject parent, IEnumerable<GameObject> siblings) {
            var source = new List<ConstraintSource>() { new ConstraintSource {
                sourceTransform = parent.transform,
                weight = 1.0f,
            }};

            foreach (var sibling in siblings) {
                var constraint = sibling.GetComponent<RotationConstraint>();

                if (!constraint)
                    constraint = sibling.AddComponent<RotationConstraint>() as RotationConstraint;

                constraint.constraintActive = false;
                constraint.SetSources(source);
            }
        }

        public static void RemoveConstraints(GameObject parent, IEnumerable<GameObject> siblings) {
            foreach (var sibling in siblings) {
                var constraint = sibling.GetComponent<RotationConstraint>();
                if (!constraint)
                    continue;

                UnityEngine.Object.DestroyImmediate(constraint);
            }
        }

        public static void ActivateConstraints(GameObject parent, IEnumerable<GameObject> siblings) {
            foreach (var sibling in siblings) {
                var constraint = sibling.GetComponent<RotationConstraint>();
                var srcTransform = constraint.GetSource(0).sourceTransform;

                constraint.rotationAtRest = sibling.transform.localRotation.eulerAngles;
                constraint.rotationOffset = (Quaternion.Inverse(srcTransform.transform.localRotation) * sibling.transform.rotation).eulerAngles;

                constraint.constraintActive = true;
            }
        }
        public static GameObject FindNextInChain(GameObject obj, string name) {
            foreach (var child in obj.Children())
                if (child.name.StartsWith(name) && child != obj)
                    return child;

            return null;
        }

        public static void ExtendChain(GameObject parent, IEnumerable<GameObject> siblings, string name) {
            int depth = CountDepth(siblings);
            int currentDepth = 1;
            var currentObj = parent;

            // Traverse to end of chain
            while (currentDepth < depth) {
                var nextObj = FindNextInChain(currentObj, name);

                if (!nextObj)
                    break;

                nextObj = currentObj;
                currentDepth++;
            }

            // Generate rest of chain
            while (currentDepth < depth) {
                var nextObj = new GameObject($"{name}_{currentDepth}");

                nextObj.transform.SetParent(currentObj.transform);
                nextObj.transform.position = nextObj.Parent().transform.position;

                currentObj = nextObj;
                currentDepth++;
            }
        }

        public static int CountDepth(IEnumerable<GameObject> siblings) {
            var siblings_ = siblings;
            int depth = 0;

            while(siblings_.Count() > 0) {
                siblings_ = siblings_.SelectMany(x => x.Children());
                depth++;
            }

            return depth;
        }

        public static GameObject CreateSource(IEnumerable<GameObject> siblings) {
            var first = siblings.First();
            var parent = first.transform.parent;

            var source = new GameObject();

            source.name = Regex.Replace(first.name, @"(_?\d+)*$", "");
            source.transform.SetParent(parent);
            source.transform.position = siblings.Select(x => x.transform.position).Aggregate((a, b) => a + b) / siblings.Count();

            return source;
        }
    }
}