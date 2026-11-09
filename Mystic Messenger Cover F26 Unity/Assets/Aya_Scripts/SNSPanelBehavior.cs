using UnityEngine;

public class SNSPanelBehavior : MonoBehaviour
{

    public GameObject targetPanel;

   public void OpenPanel()
   {
     if (targetPanel != null)
     {
        targetPanel.SetActive(true);
     }
   }

   public void ClosePanel()
   {
    if (targetPanel != null)
    {
        targetPanel.SetActive(false);
    }
   }
}
