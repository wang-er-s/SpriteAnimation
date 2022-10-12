using System;
using System.Collections.Generic;
using UnityEngine;

public class ComputeBufferMultipleSprites : MonoBehaviour {
    [SerializeField]
    private Material material;
    [SerializeField]
    private int count;
    [SerializeField]
    private SpriteAnimation spriteAnimation;

    [SerializeField] private ComputeShader computeShader;
    
    private List<SpriteAnimationChunk> chunks;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
    }

    private void Start()
    {
        chunks = new List<SpriteAnimationChunk>();
        int tmpCount = count;
        while (tmpCount > 0)
        {
            if (tmpCount >= 100)
            {
                chunks.Add(new SpriteAnimationChunk(material, computeShader, 100, spriteAnimation));
            }
            else
            {
                chunks.Add(new SpriteAnimationChunk(material, computeShader, tmpCount, spriteAnimation));
            }

            tmpCount -= 100;
        }
    }

    private void Update()
    {
        for (int i = 0; i < chunks.Count; i++)
        {
            chunks[i].Update();
        }
    }

    private void OnDestroy()
    {
        foreach (var chunk in chunks)
        {
            chunk.Dispose();
        } 
    }
}