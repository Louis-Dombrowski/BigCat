using System;
using System.Drawing;
using UnityEngine;
using Color = UnityEngine.Color;

public class GuiNotification : MonoBehaviour
{
	[Header("Properties")]
	[SerializeField] private float fallSpeed = 500f;
	[SerializeField] private Color color;
	
	[Header("State")]
	[SerializeField] private RectTransform rect;
	[SerializeField] private TMPro.TextMeshProUGUI text;
	[SerializeField] private float targetHeight;
	
	void Awake()
    {
	    rect = GetComponent<RectTransform>();
	    text = GetComponent<TMPro.TextMeshProUGUI>();
	    targetHeight = rect.parent.GetComponent<RectTransform>().rect.height;
	    text.color = color;
    }

	private void OnDisable()
	{
		Destroy(gameObject);
	}

	// Update is called once per frame
    void Update()
    {
	    Vector2 pos = rect.anchoredPosition;
	    pos.y -= fallSpeed * Time.deltaTime;

	    float percentFallen = -pos.y / targetHeight;

	    if (percentFallen > 1)
	    {
		    Destroy(gameObject);
		    return;
	    }
	    
	    float alpha = 1 - percentFallen;
	    color.a = alpha;
	    text.color = color;

	    rect.anchoredPosition = pos;
    }

    public void Initialize(string t, Color c)
    {
	    if(text == null) text = GetComponent<TMPro.TextMeshProUGUI>();
	    
	    text.text = t;
	    text.color = c;
	    color = c;
    }
}
