using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    public List<Transform> spawnRegions = new List<Transform>();
    public List<Color> colors = new List<Color>();
    public Vector2 minBounds = Vector2.zero;
    public Vector2 maxBounds = Vector2.one;
    public Vector3 cameraPosition = new Vector3(0, 10, 0);

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
