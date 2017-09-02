using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class randomButtonScript : MonoBehaviour {

    public GameObject rocket;
    private Rocket_Control controller;
    private float buttonMoveAmount = 0.045f;
    private Vector3 originalPosition;
    private bool clicking = false;
    private int unclickCounter = 0;
    private bool firstClick = true;
    private string buttonText = "";

    private int randomButtonID = 0;

	// Use this for initialization
	void Start () {
        controller = rocket.GetComponent<Rocket_Control>();
        randomButtonID = controller.getRandomButtonCode();
        Debug.Log(name + " has random button code of " + randomButtonID);

        switch (randomButtonID)
        {
            case 1:
                buttonText = "VENT";
                //controller.startFuelDrain(); // Slow enough to be fixed and live, fast enough to be a problem, include a warning message
                break;
            case 2:
                buttonText = "UNVENT";
                //controller.fixFuelDrain(); // If used when fuel drain isn't on, cuts thrust
                break;
            case 3:
                buttonText = "STAGE1";
                //controller.dropStage1();
                break;
            case 4:
                buttonText = "STAGE2";
                //controller.dropStage2();
                break;
            case 5:
                buttonText = "JET";
                //controller.jettisonPlayer(); // I'd like to include death and a restart
                break;
            case 6:
                buttonText = "ABORT";
                //controller.launchEscapeSystem(); // Full shebang on this, parachutes and all
                break;
            case 7:
                buttonText = "ZERO";
                //controller.cutThrust(); // This should just turn active to false for the engine, not actually change thrust numbers
                break;
            case 8:
                buttonText = "???";
                // One that does nothing just to fuck with them
                break;
        }

        originalPosition = transform.position;
	}
	
	// Update is called once per frame
	void Update () {
        unclickSlowly();
	}

    void Activate ()
    {
        if (firstClick)
        {
            GetComponentInChildren<Text>().text = buttonText;
        }
        if (!clicking)
        {
            // If already clicking don't let them do it again

            click();
            switch (randomButtonID)
            {
                case 1:
                    controller.startFuelDrain(); // Slow enough to be fixed and live, fast enough to be a problem, include a warning message
                    break;
                case 2:
                    controller.fixFuelDrain(); // If used when fuel drain isn't on, cuts thrust
                    break;
                case 3:
                    //controller.dropStage1();
                    break;
                case 4:
                    //controller.dropStage2();
                    break;
                case 5:
                    controller.jettisonPlayer(); // I'd like to include death and a restart
                    break;
                case 6:
                    //controller.launchEscapeSystem(); // Full shebang on this, parachutes and all
                    break;
                case 7:
                    controller.cutThrust(); // This should just turn active to false for the engine, not actually change thrust numbers
                    break;
                case 8:
                    // One that does nothing just to fuck with them
                    break;
            }
        }
    }

    private void click()
    {
        originalPosition = transform.position;
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y - buttonMoveAmount, transform.localPosition.z);
        clicking = true;
    }

    private void unclickSlowly()
    {
        if(clicking)
        {
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y + (buttonMoveAmount / 10), transform.localPosition.z);
            unclickCounter++; // I'd rather do this manually cuz I don't trust unity to not move my buttons for some other reason
            if(unclickCounter == 10)
            {
                unclickCounter = 0;
                clicking = false;
            }
        }
    }
}
