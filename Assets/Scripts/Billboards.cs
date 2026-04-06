using UnityEngine;

public class Billboards : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private Transform mainCamera;
    void Start()
    {
        mainCamera = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(transform.position + mainCamera.forward);
    }
}
