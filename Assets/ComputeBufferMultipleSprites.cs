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
        for (int i = 0; i < count / 100; i++)
        {
            chunks.Add(new SpriteAnimationChunk(material, computeShader, 100, spriteAnimation));
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