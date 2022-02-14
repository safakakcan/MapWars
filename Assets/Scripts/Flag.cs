using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flag : MonoBehaviour
{
    public string owner = "";

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeOwner(string Owner)
    {
        Texture2D flag = Camera.main.GetComponent<GameState>().GetPlayerById(Owner).flag;
        GetComponent<MeshRenderer>().materials[0].mainTexture = flag;
    }
}
