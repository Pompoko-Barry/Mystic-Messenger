using UnityEngine;
using UnityEngine.InputSystem;



public class ParticlesAtClick : MonoBehaviour
{
    private Camera _mainCamera;

    public ParticleSystem clickParticlesPrefab;

    public float distanceFromCamera = 5f;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }
    
   void Update ()
   {
    // This Pointed variable will handle both mouse clicks and screen taps (for mobile)
    var pointer = Pointer.current;
    if (pointer == null || !pointer.press.wasPressedThisFrame)
        return;

    // Screen postion + a distance in front of the camera = a point in the unity/visible space
    Vector3 screenPos = pointer.position.ReadValue();
    screenPos.z = distanceFromCamera;
    Vector3 worldPos = _mainCamera.ScreenToWorldPoint(screenPos);

    // A new copy of the prefab for every click
    ParticleSystem burst = Instantiate(clickParticlesPrefab, worldPos, Quaternion.identity);
    burst.Play();

     // Remove the copy once its particles have finished, so they don't pile up
    var main = burst.main;
    Destroy(burst.gameObject, main.duration + main.startLifetime.constantMax);
    
   }

}
