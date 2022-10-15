    using System;
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using UnityEngine;

    [CreateAssetMenu(fileName = "SpriteAnimation",menuName = "贴图动画")]
    public class SpriteAnimation : ScriptableObject
    {
        [LabelText("贴图")]
        public Texture TargetTexture;
        [LabelText("每个块渲染的数量")]
        public int PerChunkRenderCount = 100;
        [LabelText("默认动画索引")]
        public int DefaultAnimation;
        [LabelText("缩放")]
        public float Scale = 1;
        [LabelText("每帧的播放时间")]
        [Range(0,1)]
        public float UpdateTime = 0.1f;
        [LabelText("横纵向数量")]
        public Vector2Int XYCount;
#if UNITY_EDITOR
        private string TotalCountTips => $"左下角索引为0，右上角为{XYCount.x * XYCount.y - 1}";
#endif
        [InfoBox("$TotalCountTips")]
        [ListDrawerSettings(AlwaysAddDefaultValue = true, ShowIndexLabels = true)]
        public List<SpriteAnimationClip> Clips;

    }

    [Serializable]
    public struct SpriteAnimationClip
    {
        [LabelText("动画名字")] public string Name;

        [InfoBox("动画包含最大最小索引值")]
        [InfoBox("结束的索引必须大于开始的索引", InfoMessageType.Error, VisibleIf = "@EndUvIndex < StartUvIndex")]
        [LabelText("起始索引")]
        public int StartUvIndex;

        [LabelText("结束索引")] public int EndUvIndex;
    } 