using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Random = System.Random;

public class GuiData : MonoBehaviour
{
	public static GuiData instance;

	[Header("Parts")]
	[SerializeField] private GameObject guiNotifPrefab;
	[SerializeField] private Transform scoreNotifRegion;
	[SerializeField] private Transform highScoreNotifRegion;
	[SerializeField] private TMPro.TextMeshProUGUI scoreText;
	[SerializeField] private GameObject highScoreRegion;
	[SerializeField] private TMPro.TextMeshProUGUI highScoreText;
	
	[Header("State")]
	[SerializeField] private int highScore = int.MinValue;
	
	public void UpdateHighScore()
	{
		if (score <= highScore) return;

		highScore = score;
		highScoreRegion.SetActive(true);
		highScoreText.text = scoreText.text;
		
		Instantiate(guiNotifPrefab, highScoreNotifRegion).GetComponent<GuiNotification>().Initialize("New High Score!", Color.green);
	}
	
	[Header("State")]
	[SerializeField] private int _score;
	public int score
	{
		get => _score;
		set
		{
			if (scoreNotifRegion.gameObject.activeInHierarchy) // Spawn notifications if the play mode gui is active
			{
				int delta = value - _score;

				string notifText;
				Color notifColor;
				if (delta == 0)
				{
					notifText = "";
					notifColor = Color.white;
				}
				else if (delta > 0)
				{
					notifText = "+$" + delta.ToString("N0");
					notifColor = Color.green;
				}
				else
				{
					notifText = "-$" + (-delta).ToString("N0");
					notifColor = Color.red;
				}

				Instantiate(guiNotifPrefab, scoreNotifRegion).GetComponent<GuiNotification>().Initialize(notifText, notifColor);
			}
			_score = value;
			RedrawScore();
		}
	}
	
	private void Start()
	{
		if(instance != null) Destroy(instance);
		instance = this;
	}
	private void RedrawScore()
	{
		string newText = score.ToString("N0");
		
		int dollarIdx;
		if (newText[0] == '-') dollarIdx = 1;
		else dollarIdx = 0;
		newText = newText.Insert(dollarIdx, "$");

		scoreText.text = newText;
	}
}
