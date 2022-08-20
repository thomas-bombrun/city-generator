using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class CityBuilder : MonoBehaviour
{

    public static T SafeDestroy<T>(T obj) where T : Object
    {
        if (Application.isEditor)
            Object.DestroyImmediate(obj);
        else
            Object.Destroy(obj);

        return null;
    }


    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            SafeDestroy(transform.GetChild(i).gameObject);

        }
    }

    private GameObject RandomPrefab()
    {
        return prefabs[Random.Range(0, prefabs.Count)];
    }

    private GameObject CreateBuilding(GameObject prefab)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.parent = transform;
        return go;
    }

    private void LoadPrefabs()
    {
        prefabs = new List<GameObject>();
        Debug.Log("Loading all prefabs");
        string[] guids = AssetDatabase.FindAssets("t:prefab", new string[] { "Assets/Buildings" });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            prefabs.Add(go);
        }

    }

    List<GameObject> prefabs = new List<GameObject>();
    public Vector3 blockSize;
    public Vector3Int numberOfBlocks;
    public float streetSize;
    public void GenerateCity()
    {
        Clear();

        // Todo : not reload at each call
        LoadPrefabs();

        transform.position = Vector3.zero;
        for (int i = 0; i < numberOfBlocks.x; i++)
            for (int j = 0; j < numberOfBlocks.z; j++)
            {
                {
                    var block = GenerateBlock(blockSize);
                    block.transform.parent = transform;
                    block.transform.position = Vector3.right * ((blockSize.x * i) + (streetSize * i)) + Vector3.forward * ((blockSize.z * j) + (streetSize * j));
                }
            }

        //TODO : this is needed to center the buildings at (0,0,0) because the city is not generated around the CityBuilder location : it should
        transform.position = new Vector3(
            (blockSize.x * numberOfBlocks.x) + (streetSize * numberOfBlocks.x - 1),
            0,
            (blockSize.z * numberOfBlocks.z) + (streetSize * numberOfBlocks.z - 1)
            );
        transform.position *= -0.5f;

    }

    public GameObject GenerateBlock(Vector3 size)
    {
        var block = new GameObject("Block");

        var firstLine = GenerateLine(size.x);
        firstLine.transform.parent = block.transform;

        var secondLine = GenerateLine(size.x);
        secondLine.transform.parent = block.transform;
        secondLine.transform.Rotate(Vector3.up, 180f);
        secondLine.transform.position = size;
        secondLine.name = "Second Face";

        var thirdLine = GenerateLine(size.z);
        thirdLine.transform.parent = block.transform;
        thirdLine.transform.Rotate(Vector3.up, -90);
        thirdLine.transform.position = Vector3.right * size.x;
        thirdLine.name = "Third Face";

        var fourthLine = GenerateLine(size.z);
        fourthLine.transform.parent = block.transform;
        fourthLine.transform.Rotate(Vector3.up, 90);
        fourthLine.transform.position = Vector3.forward * size.z;
        fourthLine.name = "Fourth Face";
        return block;
    }

    // Generates a lign of buildings aligned on the x axis
    public GameObject GenerateLine(float length)
    {
        List<GameObject> prefabsChosen = new List<GameObject>();
        var xProgress = 0f;
        while (xProgress < length)
        {
            var pref = RandomPrefab();
            var bc = pref.GetComponent<BoxCollider>();

            var newSize = xProgress + bc.size.x;
            if (newSize > length)
            {
                break; // the next buildings would make the line too long, we stop
            }
            xProgress = newSize;

            prefabsChosen.Add(pref);
        }

        // the spacing to put between each buildings to reach size.x
        var xSpacing = (length - xProgress) / (prefabsChosen.Count - 1);

        var line = new GameObject("First Face");
        xProgress = 0;
        for (int i = 0; i < prefabsChosen.Count; i++)
        {
            var building = CreateBuilding(prefabsChosen[i]);
            var bc = building.GetComponent<BoxCollider>();
            building.transform.parent = line.transform;

            xProgress += (bc.size.x / 2);
            if (i > 0)
                xProgress += xSpacing;
            var zOffset = (bc.size.z / 2) - bc.center.z;

            building.transform.position = Vector3.right * (xProgress - bc.center.x) + Vector3.forward * zOffset;

            xProgress += bc.size.x / 2;
        }
        return line;
    }
}
