using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCor : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("start ");
        StartCoroutine(temp());
        Debug.Log("finish");
    }

    IEnumerator temp()
    {
        while(true)
        {
            Debug.Log("wxl");
        }
        yield return null;
        Debug.Log("wxl finish");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
