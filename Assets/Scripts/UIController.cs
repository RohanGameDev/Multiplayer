using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class UIController : MonoBehaviour
{
    public static UIController instance;
    public Slider weaponTemp;


    void Awake()
    {
        instance = this;
    }

    public TMP_Text overheatedText;
    public GameObject deathScreen;
    public TMP_Text deathText;
    public Slider healthSlider;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
