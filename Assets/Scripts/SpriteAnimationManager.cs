using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class SpriteAnimationManager : MonoBehaviour {

    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private Material material;

    private Dictionary<SpriteAnimation, List<SpriteAnimationChunk>> chunks = new();

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
    }

    public PoolList<int> AddSpriteAnimation(PoolList<float2> pos, PoolList<float> rotate, SpriteAnimation anim)
    {
        if (!chunks.TryGetValue(anim, out var chunkList))
        {
            chunkList = new ();
            chunks[anim] = chunkList;
        }
        
        PoolList<int> result = PoolList<int>.Create();

        int count = pos.Count;
        while (count > 0)
        {
            SpriteAnimationChunk useChunk = null;
            int order = 0;
            foreach (var chunk in chunkList)
            {
                order = chunk.Order;
                if (!chunk.Full)
                {
                    useChunk = chunk;
                    break;
                }
            }

            if (useChunk == null)
            {
                useChunk = new SpriteAnimationChunk(material, computeShader, anim,
                    order + anim.PerChunkRenderCount);
                chunkList.Add(useChunk);
            }

            count -= useChunk.LeftCapacity;
            
            // if can insert one chunk
            if (count <= 0)
            {
                result.AddRange(useChunk.SetData(pos, rotate));
            }
            // or create a new pos rotate list
            else
            {
                using var tmpPos = PoolList<float2>.Create(useChunk.LeftCapacity);
                tmpPos.AddRange(pos.GetRange(count, useChunk.LeftCapacity));
                pos.RemoveRange(count, useChunk.LeftCapacity);
                using var tmpRot = PoolList<float>.Create(useChunk.LeftCapacity);
                tmpRot.AddRange(rotate.GetRange(count, useChunk.LeftCapacity));
                rotate.RemoveRange(count, useChunk.LeftCapacity);
                
                result.AddRange(useChunk.SetData(tmpPos, tmpRot));
            }
        }

        return result;
    }


    public void ChangeAnimation(SpriteAnimation anim, PoolList<int> indexes, string animName)
    {
        
        if(!chunks.TryGetValue(anim, out var spriteChunks))
        {
            Debug.LogError($"can not find target sprite animation : {anim}");
            return;
        }
        
        using PoolDic<int, PoolList<int>> layer2ChunkIndex = PoolDic<int, PoolList<int>>.Create(); 
        foreach (var index in indexes)
        {
            var baseOrder = index / anim.PerChunkRenderCount * anim.PerChunkRenderCount;
            if (!layer2ChunkIndex.TryGetValue(baseOrder, out var list))
            {
                list = PoolList<int>.Create();
                layer2ChunkIndex[baseOrder] = PoolList<int>.Create();
            }
            list.Add(index);
        }

        foreach (var spriteChunk in spriteChunks)
        {
            if (layer2ChunkIndex.TryGetValue(spriteChunk.Order, out var chunkIndex))
            {
                spriteChunk.SetAnim(chunkIndex, animName);
                chunkIndex.Dispose();
            }
        }        
    }
    
    private void Update()
    {
        foreach (var chunksValue in chunks.Values)
        {
            foreach (var spriteAnimationChunk in chunksValue)
            {
                spriteAnimationChunk.Update();
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var chunkList in chunks.Values)
        {
            foreach (var chunk in chunkList)
            {
                chunk.Dispose();
            }
        } 
    }
}