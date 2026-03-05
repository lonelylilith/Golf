using UnityEngine;

namespace Golf
{
    [RequireComponent(typeof(Camera))]
    public class CopyParentCameraProperties : MonoBehaviour
    {
        private Camera parentCam;
        private Camera cam;

        void Start()
        {
            parentCam = transform.parent.GetComponent<Camera>();
            cam = GetComponent<Camera>();
        }

        void Update()
        {
            if(!parentCam)
                return;

            cam.orthographicSize = parentCam.orthographicSize;
        }
    }
}