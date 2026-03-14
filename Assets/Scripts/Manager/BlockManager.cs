using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BlockManager : MonoBehaviour
{
    public static BlockManager instance;

    public List<GameObject> blockList = new List<GameObject>();

    public ObjectPool<GameObject> blockPool;

    public int defaultSize;

    public int maxSize;
    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }

        blockPool = new ObjectPool<GameObject>(Create, ActionOnGet, ActionOnRelease, ActionOnDestory, true, defaultSize, maxSize);
    }

    private  GameObject Create()
    {
        GameObject block = GameObject.Instantiate(blockList[Random.Range(0, 3)]);

        return block;
    }

    private void ActionOnGet(GameObject block)
    {
        block.SetActive(true);
    }

    private void ActionOnRelease(GameObject block)
    {
        block.SetActive(false);
    }

    private void ActionOnDestory(GameObject block)
    {
        Destroy(block);
    }
}
