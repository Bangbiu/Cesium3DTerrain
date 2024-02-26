using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitCamScript : MonoBehaviour
{

    public GameObject target;

    public float sensitivity = 2.0f;

    float radius = 15f, angleX = 45f, angleY = -45f;

    void Start() {
        /*
        Component[] components = pickingInput.transform.GetChild(2).gameObject.GetComponents(typeof(Component));
        foreach(Component component in components) {
            Debug.Log(component.ToString());
        }
        */
    }
   
    void Update()
    {
        // Oribiting Terrain
        radius -= Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * 200f;
        if (Input.GetAxis("Fire2") > .0f) {
            angleX -= Input.GetAxis("Mouse X") * Time.deltaTime * sensitivity;
            angleY -= Input.GetAxis("Mouse Y") * Time.deltaTime * sensitivity;
        }

 
        float x = radius * Mathf.Cos(angleX) * Mathf.Sin(angleY);
        float z = radius * Mathf.Sin(angleX) * Mathf.Sin(angleY);
        float y = radius * Mathf.Cos(angleY);
        transform.position = new Vector3(x + target.transform.position.x,
                                         y + target.transform.position.y,
                                         z + target.transform.position.z);
        transform.LookAt(target.transform.position);

        // Cast Pinpoints
        if (Input.GetAxis("Fire1") > .0f) {

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 1000))
            {

                TerrainGenScript terrain = hit.transform.gameObject.GetComponent<TerrainGenScript>();
                if (terrain != null) {
                    terrain.Pinpoint(hit.point);
                }

                //Debug.Log((lastHit - hit.point).magnitude);
            }
        }
        if (Input.GetButtonUp("Fire1")) {

        }

    }
}
