using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TipScript : MonoBehaviour
{
    public GameObject target;
    public GameObject textBox;
    public GameObject pointPrefab;

    public Vector2Int gridPos;
    // Start is called before the first frame update
    void Start()
    {
        target = GameObject.Find("Main Camera");
        Update();
    }

    // Update is called once per frame
    void Update()
    {
        if(target != null) { 
            transform.LookAt(target.transform); 
            transform.eulerAngles = new Vector3(
                0,
                transform.eulerAngles.y,
                transform.eulerAngles.z
            );
        }
    }

    public void SetText(string text) {
        textBox.GetComponent<TextMeshProUGUI>().text = text;
    }

    public void Tip(Vector3 pos, Vector2Int gridPos, string text) {
        transform.position = pos;
        this.gridPos = gridPos;
        SetText(text);
    }
}
