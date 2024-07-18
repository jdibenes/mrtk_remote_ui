
using UnityEngine;

public class ButtonEvent : MonoBehaviour
{
    public bool pressed = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnButtonPress()
    {
        pressed = true;
    }
}
