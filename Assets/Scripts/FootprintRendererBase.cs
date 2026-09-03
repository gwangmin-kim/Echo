using UnityEngine;

namespace Echo.Gameplay
{
    public abstract class FootprintRendererBase : MonoBehaviour
    {
        public abstract void Spawn(in FootstepEventData data);
    }
}
