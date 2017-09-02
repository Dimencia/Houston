using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchButton : MonoBehaviour {

	public void Activate()
    {
        Debug.Log("Activated!");
        GameObject.Find("RocketShip").SendMessage("Launch");
    }

    public void OnMouseEnter()
    {
        //GetComponent<Renderer>().material.shader = Shader.Find("Self-Illumin/Outlined Diffuse");
        // Moved to LineOfSight
    }

    public void OnMouseExit()
    {
        //GetComponent<Renderer>().material.shader = Shader.Find("Standard");
    }
}
