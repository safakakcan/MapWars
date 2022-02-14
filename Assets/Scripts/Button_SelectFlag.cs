using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Button_SelectFlag : MonoBehaviour
{
    public int index = 0;

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
        Camera.main.GetComponent<PlayerController>().UIRegister_SelectFlag(index);
    }
}
