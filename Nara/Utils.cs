using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace Nara
{
    public static class Utils {
        public static IEnumerable<GameObject> Children(this GameObject parent)
            => parent.GetComponentsInChildren<Transform>().Select(x => x.gameObject).Where(x => x.Parent() == parent);

        public static GameObject Parent(this GameObject child)
            => child.transform.parent.gameObject;

        public static void Activate(this RotationConstraint constraint) {
                constraint.constraintActive = false;

                List<ConstraintSource> sources = new List<ConstraintSource>();
                constraint.GetSources(sources);

                constraint.rotationAtRest = Vector3.zero;
                constraint.rotationOffset = Vector3.zero;

                foreach (var source in sources) {
                    constraint.rotationAtRest = constraint.transform.localRotation.eulerAngles;
                    constraint.rotationOffset = (Quaternion.Inverse(source.sourceTransform.localRotation) * constraint.transform.rotation).eulerAngles;
                }

                constraint.constraintActive = true;
        }
    }
}