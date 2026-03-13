using UnityEngine;
using UnityEngine.Pool;

public enum BlockType
{
    Building, // 放置的方块（地面建筑物）
    Pickup    // 可拾取物（掉落物品）
}

public class BlockManager : MonoBehaviour
{
    public static BlockManager instance;

    [Header("放置方块预制体")]
    public GameObject buildingTrianglePrefab;
    public GameObject buildingSquarePrefab;
    public GameObject buildingCirclePrefab;

    [Header("可拾取物预制体")]
    public GameObject pickupTrianglePrefab;
    public GameObject pickupSquarePrefab;
    public GameObject pickupCirclePrefab;

    private ObjectPool<GameObject> buildingTrianglePool;
    private ObjectPool<GameObject> buildingSquarePool;
    private ObjectPool<GameObject> buildingCirclePool;
    private ObjectPool<GameObject> pickupTrianglePool;
    private ObjectPool<GameObject> pickupSquarePool;
    private ObjectPool<GameObject> pickupCirclePool;

    public int defaultSize = 10;
    public int maxSize = 20;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        buildingTrianglePool = CreatePool(buildingTrianglePrefab);
        buildingSquarePool = CreatePool(buildingSquarePrefab);
        buildingCirclePool = CreatePool(buildingCirclePrefab);

        pickupTrianglePool = CreatePool(pickupTrianglePrefab);
        pickupSquarePool = CreatePool(pickupSquarePrefab);
        pickupCirclePool = CreatePool(pickupCirclePrefab);
    }

    private ObjectPool<GameObject> CreatePool(GameObject prefab)
    {
        return new ObjectPool<GameObject>(
            createFunc: () => Instantiate(prefab),
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyObj,
            collectionCheck: true,
            defaultCapacity: defaultSize,
            maxSize: maxSize
        );
    }

    private void OnGet(GameObject obj) => obj.SetActive(true);
    private void OnRelease(GameObject obj) => obj.SetActive(false);
    private void OnDestroyObj(GameObject obj) => Destroy(obj);

    public GameObject GetBlock(ShapeType shape, BlockType type)
    {
        switch (type)
        {
            case BlockType.Building:
                return GetBuildingBlock(shape);
            case BlockType.Pickup:
                return GetPickupBlock(shape);
            default:
                return null;
        }
    }

    private GameObject GetBuildingBlock(ShapeType shape)
    {
        switch (shape)
        {
            case ShapeType.Triangle: return buildingTrianglePool.Get();
            case ShapeType.Square: return buildingSquarePool.Get();
            case ShapeType.Circle: return buildingCirclePool.Get();
            default: return null;
        }
    }

    private GameObject GetPickupBlock(ShapeType shape)
    {
        switch (shape)
        {
            case ShapeType.Triangle: return pickupTrianglePool.Get();
            case ShapeType.Square: return pickupSquarePool.Get();
            case ShapeType.Circle: return pickupCirclePool.Get();
            default: return null;
        }
    }

    public void ReturnBlock(GameObject block, ShapeType shape, BlockType type)
    {
        block.SetActive(false);
        switch (type)
        {
            case BlockType.Building:
                ReturnBuildingBlock(block, shape);
                break;
            case BlockType.Pickup:
                ReturnPickupBlock(block, shape);
                break;
        }
    }

    private void ReturnBuildingBlock(GameObject block, ShapeType shape)
    {
        switch (shape)
        {
            case ShapeType.Triangle: buildingTrianglePool.Release(block); break;
            case ShapeType.Square: buildingSquarePool.Release(block); break;
            case ShapeType.Circle: buildingCirclePool.Release(block); break;
        }
    }

    private void ReturnPickupBlock(GameObject block, ShapeType shape)
    {
        switch (shape)
        {
            case ShapeType.Triangle: pickupTrianglePool.Release(block); break;
            case ShapeType.Square: pickupSquarePool.Release(block); break;
            case ShapeType.Circle: pickupCirclePool.Release(block); break;
        }
    }
}