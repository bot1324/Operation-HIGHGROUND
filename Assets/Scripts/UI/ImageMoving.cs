﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageMoving : MonoBehaviour
{
    private Image image;
    private RectTransform currentPosition;
    private float x;
    private float newY;
    // Start is called before the first frame update
    void Start()
    {
        image = GetComponent<Image>();
        currentPosition = GetComponent<RectTransform>();
        x = currentPosition.anchoredPosition.x;
    }

    public void NewYValue(float v)
    {
        newY = (-226.2f + (v - 50) * 6.287f) + 33;
        currentPosition.anchoredPosition = new Vector2(x, newY);
    }

    // Update is called once per frame
    void Update()
    {
        //I hate sand
    }  
}
