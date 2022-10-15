using System;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class Launch : MonoBehaviour
{

    [SerializeField] private int count;
    [SerializeField] private SpriteAnimation spriteAnimation;
    private SpriteAnimationManager spriteAnimationManager;
    private PoolList<int> animIndex;

    private void Start()
    {
        spriteAnimationManager = GetComponent<SpriteAnimationManager>();
        using PoolList<float2> pos = PoolList<float2>.Create(count);
        using PoolList<float> rot = PoolList<float>.Create(count);
        Random random = Random.CreateFromIndex((uint)UnityEngine.Random.Range(1, 10000));
        for (int i = 0; i < count; i++)
        {
            pos.Add(random.NextFloat2(new float2(-10, -5), new float2(10, 5)));
            rot.Add(random.NextFloat(-45, 45));
        }

        animIndex = spriteAnimationManager.AddSpriteAnimation(pos, rot, spriteAnimation);
    }

    private void OnGUI()
    {
        int height = 100;
        foreach (var animationClip in spriteAnimation.Clips)
        {
            if (GUI.Button(new Rect(100, height += 100, 100, 50), animationClip.Name))
            {
                spriteAnimationManager.ChangeAnimation(spriteAnimation, animIndex, animationClip.Name);
            }
        }
    }
}