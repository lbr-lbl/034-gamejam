using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplitScreenController : MonoBehaviour
{
    public Camera camP1;
    public Camera camP2;
    public Transform player1;
    public Transform player2;
    public RectTransform divider;

    public float initialFixedDuration = 5f;
    public float fullMapDuration = 2f;
    public float smoothTime = 0.3f;

    private float mapXMin = 0f, mapXMax = 20f, mapYMin = 0f, mapYMax = 10f;
    private float baseSize = 5f;
    private enum Stage { InitialFixed, FullMap, Following }
    private Stage currentStage = Stage.InitialFixed;
    private Vector3 velCamP1, velCamP2;
    private float lastAspect;

    void Start()
    {
        StartCoroutine(StageRoutine());
        SetInitialFixedPositions();
        lastAspect = GetScreenAspect();
    }

    void Update()
    {
        float currentAspect = GetScreenAspect();
        if (Mathf.Abs(currentAspect - lastAspect) > 0.01f)
        {
            lastAspect = currentAspect;
            AdjustCameraSizeForStage();
        }

        if (currentStage == Stage.Following)
            FollowPlayers();
    }

    float GetScreenAspect() => (float)Screen.width / Screen.height;

    void AdjustCameraSizeForStage()
    {
        float aspect = GetScreenAspect();
        float targetSize = baseSize;
        if (currentStage == Stage.FullMap)
        {
            float neededForWidth = mapXMax / aspect;
            float neededForHeight = mapYMax / 2f;
            targetSize = Mathf.Max(neededForWidth, neededForHeight);
        }
        camP1.orthographicSize = targetSize;
        camP2.orthographicSize = targetSize;
    }

    IEnumerator StageRoutine()
    {
        currentStage = Stage.InitialFixed;
        divider.gameObject.SetActive(true);
        SetInitialFixedPositions();
        AdjustCameraSizeForStage();
        yield return new WaitForSeconds(initialFixedDuration);

        currentStage = Stage.FullMap;
        divider.gameObject.SetActive(false);
        SetFullMapPositions();
        AdjustCameraSizeForStage();
        yield return new WaitForSeconds(fullMapDuration);

        currentStage = Stage.Following;
        divider.gameObject.SetActive(true);
        camP1.orthographicSize = baseSize;
        camP2.orthographicSize = baseSize;
    }

    void SetInitialFixedPositions()
    {
        camP1.transform.position = new Vector3(2.5f, 5f, -10);
        camP2.transform.position = new Vector3(17.5f, 5f, -10);
    }

    void SetFullMapPositions()
    {
        Vector3 center = new Vector3(10f, 5f, -10);
        camP1.transform.position = center;
        camP2.transform.position = center;
    }

    void FollowPlayers()
    {
        Vector3 targetP1 = new Vector3(player1.position.x, player1.position.y, -10);
        Vector3 targetP2 = new Vector3(player2.position.x, player2.position.y, -10);
        targetP1 = ClampCameraPosition(targetP1, camP1);
        targetP2 = ClampCameraPosition(targetP2, camP2);
        camP1.transform.position = Vector3.SmoothDamp(camP1.transform.position, targetP1, ref velCamP1, smoothTime);
        camP2.transform.position = Vector3.SmoothDamp(camP2.transform.position, targetP2, ref velCamP2, smoothTime);
    }

    Vector3 ClampCameraPosition(Vector3 target, Camera cam)
    {
        float vertExtent = cam.orthographicSize;
        float horzExtent = vertExtent * cam.aspect;
        float minX = mapXMin + horzExtent;
        float maxX = mapXMax - horzExtent;
        float minY = mapYMin + vertExtent;
        float maxY = mapYMax - vertExtent;

        if (minX > maxX) minX = maxX = (mapXMin + mapXMax) / 2f;
        if (minY > maxY) minY = maxY = (mapYMin + mapYMax) / 2f;

        target.x = Mathf.Clamp(target.x, minX, maxX);
        target.y = Mathf.Clamp(target.y, minY, maxY);
        return target;
    }
}