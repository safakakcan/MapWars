using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Navigator : MonoBehaviour
{
    List<Transform> obstacles = new List<Transform>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void DetectAllObstacles()
    {
        obstacles.Clear();
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Obstacle");
        
    }
}
