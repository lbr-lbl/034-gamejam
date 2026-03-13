using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.3f;

    private float mapLeft, mapRight, mapBottom, mapTop;
    private Camera cam;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    public void SetMapBounds(float left, float right, float bottom, float top)
    {
        mapLeft = left;
        mapRight = right;
        mapBottom = bottom;
        mapTop = top;
    }

    void LateUpdate()
    {
        if (target == null || cam == null) return;

        Vector3 desiredPos = new Vector3(target.position.x, target.position.y, -10f);
        Vector3 smoothedPos = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothTime);

        float halfWidth = cam.orthographicSize * cam.aspect;
        float halfHeight = cam.orthographicSize;

        float clampedX = Mathf.Clamp(smoothedPos.x, mapLeft + halfWidth, mapRight - halfWidth);
        float clampedY = Mathf.Clamp(smoothedPos.y, mapBottom + halfHeight, mapTop - halfHeight);

        transform.position = new Vector3(clampedX, clampedY, smoothedPos.z);
    }
}