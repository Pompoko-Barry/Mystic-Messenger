using UnityEngine;
using TMPro;


public class InGame_Settings : MonoBehaviour
{

    int speedLevel = 0;
    public float slowSpeed = 0.5f;
    public float fastSpeed = 3f;



void ApplySpeed()
{
    if (speedLevel <= -1)
    {
        Time.timeScale = slowSpeed;
    }
    else if (speedLevel == 0)
    {
        Time.timeScale = 1f;
    }
    else
    {
        Time.timeScale = fastSpeed;
    }
}

void Start()
{
   
    ApplySpeed();
   
}

public void SpeedUp()
{
    
    speedLevel += 1; 
    float limited = Mathf.Clamp(speedLevel, -1, 1);
    ApplySpeed();
    
    
}


public void SlowDown()
{
    speedLevel -= 1; 
    float limited = Mathf.Clamp(speedLevel, -1, 1);
    ApplySpeed();
    
}

public void NormalSpeed()
{
    speedLevel = 0; 
    ApplySpeed();
}
}
