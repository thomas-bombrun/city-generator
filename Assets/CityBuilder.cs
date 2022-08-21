using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MultiValueDictionary<Key, Value> : Dictionary<Key, List<Value>>
{
    public void Add(Key key, Value value)
    {
        List<Value> values;
        if (!this.TryGetValue(key, out values))
        {
            values = new List<Value>();
            this.Add(key, values);
        }
        values.Add(value);
    }
}
public enum cityBuildType
{
    Static,
    Split,
};

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

    private GameObject RandomBuildingPrefab()
    {
        return buildingPrefabs[Random.Range(0, buildingPrefabs.Count)];
    }

    private GameObject CreateBuilding(GameObject prefab)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.parent = transform;
        return go;
    }
    private GameObject CreateRandomStreetCorner()
    {
        GameObject prefab = streetCornerPrefabs[Random.Range(0, streetCornerPrefabs.Count)];
        return CreateBuilding(prefab);
    }


    public List<GameObject> buildingPrefabs = new List<GameObject>();
    public Vector3 blockSize;
    public Vector3Int numberOfBlocks;
    public cityBuildType splitType;


    public float streetSize;
    public GameObject streetPrefab;
    public List<GameObject> streetCornerPrefabs;
    public List<GameObject> streetPlaced;


    public void GenerateCity()
    {
        Clear();
        streetPlaced = new List<GameObject>();

        transform.position = Vector3.zero;
        if (splitType == cityBuildType.Static)
            StaticBuild();
        else if (splitType == cityBuildType.Split)
        {
            Vector3 fullBlockSize = new Vector3(
                (blockSize.x * numberOfBlocks.x) + (streetSize * numberOfBlocks.x - 1),
                0,
                (blockSize.z * numberOfBlocks.z) + (streetSize * numberOfBlocks.z - 1)
            );
            SplitSpace(fullBlockSize, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        }

        RemoveStreetsDuplicates();

        //TODO : this is needed to center the buildings at (0,0,0) because the city is not generated around the CityBuilder location : it should
        transform.position = new Vector3(
            (blockSize.x * numberOfBlocks.x) + (streetSize * numberOfBlocks.x - 1),
            0,
            (blockSize.z * numberOfBlocks.z) + (streetSize * numberOfBlocks.z - 1)
        );
        transform.position *= -0.5f;



    }

    void StaticBuild()
    {
        for (int i = 0; i < numberOfBlocks.x; i++)
        {
            for (int j = 0; j < numberOfBlocks.z; j++)
            {
                {
                    var block = GenerateBlock(blockSize);
                    block.transform.parent = transform;
                    block.transform.position = Vector3.right * ((blockSize.x * i) + (streetSize * i)) + Vector3.forward * ((blockSize.z * j) + (streetSize * j));
                }
            }
        }
    }


    public Vector3Int maxSplitCount;
    public void SplitSpace(Vector3 blockDimensions, Vector3 blockPosition, Vector3 splitIndex, Vector3 splitDimension)
    {
        if (Vector3.Dot(splitIndex, splitDimension) < Vector3.Dot(maxSplitCount, splitDimension))
        {
            float lengthToSplit = Vector3.Dot(blockDimensions, splitDimension);
            float splitPosition = Random.Range(lengthToSplit / 4, lengthToSplit * 3 / 4);

            Vector3 filterDimensionVector = new Vector3(1, 1, 1) - splitDimension;
            Vector3 partialDimensions = Vector3.Scale(blockDimensions, filterDimensionVector);
            Vector3 partialPositions = blockPosition;  //  - 0.5f * (lengthToSplit - streetSize) * splitDimension;

            Vector3 newSplitDimension = Vector3.Scale(Vector3.Cross(splitDimension, new Vector3(0, 1, 0)), new Vector3(-1, 0, 1));

            // First split
            Vector3 firstSubBlockDimensions = partialDimensions + splitDimension * splitPosition;
            Vector3 firstSubBlockPosition = partialPositions;  // + 0.5f * splitPosition * splitDimension;
            SplitSpace(firstSubBlockDimensions, firstSubBlockPosition, splitIndex + splitDimension, newSplitDimension);
            // Second split
            Vector3 secondSubBlockDimensions = partialDimensions + splitDimension * (lengthToSplit - splitPosition - 0.5f * streetSize);
            Vector3 secondSubBlockPosition = partialPositions + (splitPosition + streetSize) * splitDimension;
            SplitSpace(secondSubBlockDimensions, secondSubBlockPosition, splitIndex + splitDimension, newSplitDimension);
        }
        else
        {
            GameObject block = GenerateBlock(blockDimensions);
            block.transform.parent = transform;
            block.transform.position = blockPosition;
        }

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
            var pref = RandomBuildingPrefab();
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

        // We add the street

        GameObject street = CreateBuilding(streetPrefab);
        street.transform.localScale = new Vector3(length, 1, streetSize);
        street.transform.position = new Vector3(length / 2, 0, -streetSize / 2);
        street.transform.parent = line.transform;
        streetPlaced.Add(street);


        GameObject streetCorner = CreateRandomStreetCorner();
        streetCorner.transform.localScale = new Vector3(streetSize, 1, streetSize);
        streetCorner.transform.position = new Vector3(-streetSize / 2, 0, -streetSize / 2);
        streetCorner.transform.parent = line.transform;
        streetPlaced.Add(streetCorner);

        return line;
    }
    private void RemoveStreetsDuplicates()
    {
        for (int i = 0; i < streetPlaced.Count; i++)
        {
            for (int j = 0; j < streetPlaced.Count; j++)
            {
                if (i != j && streetPlaced[i] != null && streetPlaced[j] != null && streetPlaced[i].transform.position == streetPlaced[j].transform.position)
                {
                    SafeDestroy(streetPlaced[i]);
                }
            }
        }
    }

}
