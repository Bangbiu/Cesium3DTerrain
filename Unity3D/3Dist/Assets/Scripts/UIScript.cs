using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIScript : MonoBehaviour
{
    [SerializeField]
    public GameObject[] targetInputs;

    public GameObject terrainObject;

    public int pickingIndex
    {
        get => _pickingIndex;
        set
        {
            _pickingIndex = value;
            pickingInput = targetInputs[value];
        }
    }

    private GameObject pickingInput;
    private TerrainGenScript terrain;

    private int _pickingIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        terrain = terrainObject.GetComponent<TerrainGenScript>();
        onTargetPick(0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private Vector2Int getInputPosition(int index) {
        GameObject target = targetInputs[index];
        string iX = target.transform.GetChild(2).gameObject.GetComponent<TMP_InputField>().text;
        string iZ = target.transform.GetChild(3).gameObject.GetComponent<TMP_InputField>().text;
        int x  = iX == "" ? 0 : int.Parse(iX);
        int z  = iZ == "" ? 0 : int.Parse(iZ);
        return new Vector2Int(x, z);
    }

    public void setInputPosition(int x, int z) {
        pickingInput.transform.GetChild(2).gameObject.GetComponent<TMP_InputField>().text = x.ToString();
        pickingInput.transform.GetChild(3).gameObject.GetComponent<TMP_InputField>().text = z.ToString();
    }

    public void onTargetPick(int index) {
        foreach (GameObject input in targetInputs) {
            input.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.392f);
        }

        targetInputs[index].GetComponent<Image>().color = Color.yellow;
        pickingIndex = index;
    }

    public void onInputPinCoord(int index) {
        // Update Tip
        //Debug.Log(getInputPosition(index));
        terrain.Pinpoint(getInputPosition(index), index);
    }


}
