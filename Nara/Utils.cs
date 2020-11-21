using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Nara
{
    public static class Utils {
        public static IEnumerable<GameObject> Children(this GameObject parent)
            => parent.GetComponentsInChildren<Transform>().Select(x => x.gameObject).Where(x => x.Parent() == parent);

        public static GameObject Parent(this GameObject child)
            => child.transform.parent.gameObject;
    }
}