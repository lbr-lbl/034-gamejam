// BlockManager.cs
using UnityEngine;
using UnityEngine.Pool;

public class BlockManager : MonoBehaviour
{
    public static BlockManager instance;

    public GameObject trianglePrefab;
    public GameObject squarePrefab;
    public GameObject circlePrefab;

    private ObjectPool<GameObject> trianglePool;
    private ObjectPool<GameObject> squarePool;
    private ObjectPool<GameObject> circlePool;

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

        trianglePool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(trianglePrefab),
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyObj,
            collectionCheck: true,
            defaultCapacity: defaultSize,
            maxSize: maxSize
        );

        squarePool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(squarePrefab),
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyObj,
            collectionCheck: true,
            defaultCapacity: defaultSize,
            maxSize: maxSize
        );

        circlePool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(circlePrefab),
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyObj,
            collectionCheck: true,
            defaultCapacity: defaultSize,
            maxSize: maxSize
        );
    }

    public GameObject GetBlock(ShapeType shape)
    {
        switch (shape)
        {
            case ShapeType.Triangle: return trianglePool.Get();
            case ShapeType.Square: return squarePool.Get();
            case ShapeType.Circle: return circlePool.Get();
            default: return null;
        }
    }

    public void ReturnBlock(GameObject block, ShapeType shape)
    {
        block.SetActive(false);
        switch (shape)
        {
            case ShapeType.Triangle: trianglePool.Release(block); break;
            case ShapeType.Square: squarePool.Release(block); break;
            case ShapeType.Circle: circlePool.Release(block); break;
        }
    }

    private void OnGet(GameObject obj) => obj.SetActive(true);
    private void OnRelease(GameObject obj) => obj.SetActive(false);
    private void OnDestroyObj(GameObject obj) => Destroy(obj);
}