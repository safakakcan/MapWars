using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMagicCard : MonoBehaviour
{
    public string id = "";

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Select()
    {
        Camera.main.GetComponent<PlayerController>().myAssets.transform.GetChild(4).GetComponent<UIMagicCardInfo>().SelectCard(id);
    }
}
