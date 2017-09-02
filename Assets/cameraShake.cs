using UnityEngine;
using System.Collections;
public class cameraShake : MonoBehaviour
{
    private Vector3 originPosition;
    private Vector3 originRotation;
    public float shake_decay;
    public float shake_intensity;
    public double shake_delay;
    private bool delayFinished;


    private void Start()
    {
        InvokeRepeating("DelayTimer", 0, 0.1f);
        delayFinished = true;
    }


    public void UpdateCameraShake()
    {

        if (shake_intensity > 0)
        {
            Vector3 startPosition = transform.localPosition;
            Vector3 startRotation = transform.eulerAngles;
            transform.localPosition = startPosition + Random.insideUnitSphere * shake_intensity;
            transform.eulerAngles = new Vector3(
            startRotation.x + Random.Range(-shake_intensity, shake_intensity) * .2f,
            startRotation.y + Random.Range(-shake_intensity, shake_intensity) * .2f,
            startRotation.z);
            shake_intensity -= shake_decay;
            if(shake_intensity <= 0)
            {
                transform.localPosition = originPosition;
                //transform.localRotation = originRotation;
            }
        }
    }

    void Shake(Vector3 shake_data)
    {
        if (delayFinished)
        {
            originPosition = transform.localPosition;
            originRotation = transform.eulerAngles;
            shake_intensity = shake_data.x;
            shake_decay = shake_data.y;
            shake_delay = shake_data.z;
            delayFinished = false;
        }
        else
        { //Debug.Log("Ignoring shake, on delay"); 
        }
        // Ignore any shake requests if still on delay
    }

    void DelayTimer()
    {
        if (shake_delay >= 0)
        {
            delayFinished = false;
            shake_delay -= 0.1;
        }
        else
            delayFinished = true;
    }
}