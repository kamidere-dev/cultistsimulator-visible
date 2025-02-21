﻿#pragma warning disable 0649
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using SecretHistories.UI;

public class NotificationLog : MonoBehaviour {

	[SerializeField] CanvasGroupFader fader;
	[SerializeField] TextMeshProUGUI[] textMeshes;
	float[] textTime;

	private const float startFadeAt = 0.5f;
	private bool isFadingOut;

	void Start() {
		textTime = new float[textMeshes.Length];

		for (int i = 0; i < textTime.Length; i++) {
			textTime[i] = 0f;
			textMeshes[i].gameObject.SetActive(false);
		}

		fader.SetStatesForFinalAlpha(0f);
	}

	public void AddText(string text) {
        //Use this <color=#f6a2e2>Color to highlight</color> special game terms (just put it in the string)
        int index = GetFreeTextMeshIndex();

		textMeshes[index].text = text;
		textMeshes[index].gameObject.SetActive(true);
		textMeshes[index].transform.SetAsLastSibling();
		textMeshes[index].canvasRenderer.SetAlpha(1f);
		textTime[index] = 3f;

		fader.Show();
		isFadingOut = false;
	}

	int GetFreeTextMeshIndex() {		
		float smallestTime = Mathf.Infinity;
		int smallestIndex = 0;

		for (int i = 0; i < textMeshes.Length; i++) {
			if (textTime[i] <= 0f)
				return i;
			else if (textTime[i] < smallestTime) {
				smallestTime = textTime[i];
				smallestIndex = i;
			}
		}

		return smallestIndex;
	}

	void Update() {
		bool hasActiveMesh = false;

		for (int i = 0; i < textMeshes.Length; i++) {
			if (textMeshes[i].gameObject.activeInHierarchy == false)
				continue;

			hasActiveMesh = true;
			textTime[i] -= Time.deltaTime;

			if (textTime[i] > startFadeAt)
				continue;
			else if (textTime[i] <= 0f)
				textMeshes[i].gameObject.SetActive(false);
			else 
				textMeshes[i].canvasRenderer.SetAlpha(textTime[i] / startFadeAt);
		}

		if (hasActiveMesh == false && !isFadingOut) {
			isFadingOut = true;
			fader.Hide();
		}
	}
}
