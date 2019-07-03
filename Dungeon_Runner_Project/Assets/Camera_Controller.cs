using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Controller : MonoBehaviour {

    [SerializeField] private GameObject target_obj;
	// Use this for initialization
	void Start ()
    {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        // Follows the object set as the target object
        Vector3 set_position = target_obj.transform.position + new Vector3(0.0f, 0.0f, -10.0f);
        transform.position = set_position;

    }
}
