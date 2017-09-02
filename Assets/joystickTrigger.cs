using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class joystickTrigger : MonoBehaviour {

    public GameObject rocket;
    private Rocket_Control controls;

	// Use this for initialization
	void Start () {
        controls = rocket.GetComponent<Rocket_Control>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void Activate()
    {
        controls.enterCockpit();
    }
}
