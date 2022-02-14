using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Region : MonoBehaviour
{
    public string id = "";
    public string owner = "";
    public List<Region> neighbors = new List<Region>();
    public bool selected = false;

    // Start is called before the first frame update
    void Start()
    {
        transform.GetChild(0).GetComponent<TMPro.TextMeshPro>().text = id;
    }

    // Update is called once per frame
    void Update()
    {
        if (selected)
        {
            transform.position = Vector3.MoveTowards(transform.position, 
                new Vector3(transform.position.x, 25f, transform.position.z), Time.deltaTime * 200);
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, 
                new Vector3(transform.position.x, 0, transform.position.z), Time.deltaTime * 200);
        }
    }

    public void ChangeOwner(string newOwner = "")
    {
        owner = newOwner;
        if (newOwner == "")
        {
            SetColor(Color.white);
        }
        else
        {
            SetColor(Camera.main.GetComponent<GameState>().GetPlayerById(newOwner).color);
        }
    }

    private Color GetColor()
    {
        return GetComponent<Renderer>().material.color;
    }

    private void SetColor(Color color)
    {
        Color newColor = new Color(color.r, color.g, color.b, 0.2f);
        GetComponent<Renderer>().material.color = newColor;
    }

    public bool IsRegionNeighbor(string id)
    {
        foreach (Region region in neighbors)
        {
            if (region.id == id)
            {
                return true;
            }
        }

        return false;
    }
}
