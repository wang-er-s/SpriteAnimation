    using System;
    using System.Collections.Generic;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Rendering;
    using Object = UnityEngine.Object;

    public class SpriteAnimationChunk : IDisposable
    {

        private Material mat;
        private ComputeShader computeShader;
        private int CurrentCount { get; set; }
        private int MaxCount => spriteAnimation.PerChunkRenderCount;
        public int LeftCapacity => MaxCount - CurrentCount;
        public bool Full => CurrentCount >= MaxCount;
        private SpriteAnimation spriteAnimation;

        #region buffer

        private Mesh mesh;

        // Matrix here is a compressed transform information
        // xy is the position, z is rotation, w is the scale
        private ComputeBuffer transformBuffer;

        // uvBuffer contains float4 values in which xy is the uv dimension and zw is the texture offset
        private ComputeBuffer uvBuffer;
        private ComputeBuffer scaleBuffer;
        private ComputeBuffer animIndexBuffer;
        private ComputeBuffer stencilBuffer;
        private uint[] args;
        private ComputeBuffer argsBuffer;

        #endregion

        #region compute shader dispatch

        private int kernel;
        private const int XThreadCount = 256;
        private int groupX;

        #endregion

        #region 缓存的需要运行时修改的data

        // xy is pos, z is rotation
        private float3[] transformsData;
        private int[] animIndexData;
        // 0不存在 1存在
        private int[] stencilData;

        #endregion

        #region buffer名字

        private const string transformBufferPropertyName = "transformBuffer";
        private const string stencilBufferPropertyName = "stencilBuffer";
        private const string scaleBufferPropertyName = "scaleBuffer";
        private const string uvBufferPropertyName = "uvBuffer";
        private const string animIndexBufferPropertyName = "animIndexBuffer";

        private const string spritesComputeShaderName = "sprites";
        private const string animLoopComputeShaderName = "animLoop";
        private const string animRangeComputeShaderName = "animRange";
        private const string refreshComputeShaderName = "refresh";

        #endregion

        // 记录索引
        public int Order { get; }

        public SpriteAnimationChunk(Material mat, ComputeShader computeShader, SpriteAnimation spriteAnimation, int basicOrder)
        {
            Order = basicOrder;
            this.mat = new Material(mat);
            mat.mainTexture = spriteAnimation.TargetTexture;
            this.computeShader = Object.Instantiate(computeShader);
            this.spriteAnimation = spriteAnimation;
            mesh = CreateQuad();
            kernel = this.computeShader.FindKernel("Sprite");
            groupX = MaxCount / XThreadCount + 1;
            Init();
        }
        
        private static readonly Bounds BOUNDS = new Bounds(Vector2.zero, Vector3.one);
        private float animLoopTimer;
        private int animLoopIndex;
        private bool hasRefresh;

        public void Update()
        {
            animLoopTimer += Time.deltaTime;
            if (animLoopTimer >= spriteAnimation.UpdateTime)
            {
                animLoopTimer = 0;
                animLoopIndex++;
                computeShader.SetBool(refreshComputeShaderName, true);
                computeShader.SetInt(animLoopComputeShaderName, animLoopIndex);
                computeShader.Dispatch(kernel, groupX, 1, 1);
                hasRefresh = true;
            }
            else if (hasRefresh)
            {
                hasRefresh = false;
                computeShader.SetBool(refreshComputeShaderName, false);
                computeShader.Dispatch(kernel, groupX, 1, 1);
            }

            Graphics.DrawMeshInstancedIndirect(this.mesh, 0, this.mat, BOUNDS, this.argsBuffer,
                castShadows: ShadowCastingMode.Off, receiveShadows: false);
        }

        /// <summary>
        /// add new animation
        /// </summary>
        /// <returns>sprite order id, use for change animation</returns>
        /// <exception cref="Exception">data length greater than capacity</exception>
        public PoolList<int> SetData(PoolList<float2> pos, PoolList<float> rotate)
        {
            if (pos.Count != rotate.Count || pos.Count > LeftCapacity)
            {
                throw new Exception("beyond expect count!");
            }

            PoolList<int> result = PoolList<int>.Create(pos.Count);
            CurrentCount += pos.Count;

            int index = 0;
            for (int i = 0; i < stencilData.Length; i++)
            {
                if (stencilData[i] == 0)
                {
                    transformsData[i] = new float3(pos[index], rotate[index]);
                    stencilData[i] = 1;
                    result.Add(Order + i);
                    index++;
                }

                if(index >= pos.Count) break;
            }

            this.transformBuffer.SetData(transformsData);
            this.mat.SetBuffer(transformBufferPropertyName, this.transformBuffer);
            
            stencilBuffer.SetData(stencilData);
            mat.SetBuffer(stencilBufferPropertyName, stencilBuffer);
            computeShader.SetBuffer(kernel, stencilBufferPropertyName, stencilBuffer);

            return result;
        }

        private void Init()
        {
            // Prepare values
            transformsData = new float3[MaxCount];
            animIndexData = new int[MaxCount];
            var uvs = new float4[MaxCount];
            var scales = new float[MaxCount];
            stencilData = new int[MaxCount];

            transformBuffer = new ComputeBuffer(MaxCount, 4 * 3);
            scaleBuffer = new ComputeBuffer(MaxCount, 4);
            stencilBuffer = new ComputeBuffer(MaxCount, 4);
            uvBuffer = new ComputeBuffer(MaxCount, 4 * 4);
            animIndexBuffer = new ComputeBuffer(MaxCount, 4);
            
            for (int i = 0; i < MaxCount; ++i)
            {
                animIndexData[i] = spriteAnimation.DefaultAnimation;
                scales[i] = spriteAnimation.Scale;// UV
                float u = UnityEngine.Random.Range(0, 4) * 0.25f;
                float v = UnityEngine.Random.Range(0, 4) * 0.25f;
                uvs[i] = new float4(0.25f, 0.25f, u, v);
            }
            
            animIndexBuffer.SetData(animIndexData);
            computeShader.SetBuffer(kernel, animIndexBufferPropertyName, animIndexBuffer);
            
            this.uvBuffer.SetData(uvs);
            this.mat.SetBuffer(uvBufferPropertyName, this.uvBuffer);
            computeShader.SetBuffer(kernel, uvBufferPropertyName, uvBuffer);
            
            this.scaleBuffer.SetData(scales);
            mat.SetBuffer(scaleBufferPropertyName, scaleBuffer);


            var spriteCount = spriteAnimation.XYCount.x * spriteAnimation.XYCount.y;
            var spritesData = new Vector4[spriteCount];
            int index = 0;
            float uvXOffset = 1.0f / spriteAnimation.XYCount.x;
            float uvYOffset = 1.0f / spriteAnimation.XYCount.y;
            for (int i = 0; i < spriteAnimation.XYCount.x; i++)
            {
                for (int j = 0; j < spriteAnimation.XYCount.y; j++)
                {
                    spritesData[index] = new Vector4(uvXOffset, uvYOffset, i * uvXOffset, j * uvYOffset);
                    index++;
                }
            }

            computeShader.SetVectorArray(spritesComputeShaderName, spritesData);

            // 每个动画的范围赋值给compute shader
            Vector4[] animRange = new Vector4[spriteAnimation.Clips.Count];
            index = 0;
            foreach (var animationClip in spriteAnimation.Clips)
            {
                animRange[index] = new Vector4(animationClip.StartUvIndex, animationClip.EndUvIndex);
                index++;
            }

            computeShader.SetVectorArray(animRangeComputeShaderName, animRange);

            this.args = new uint[]
            {
                6, (uint)this.MaxCount, 0, 0, 0
            };
            this.argsBuffer =
                new ComputeBuffer(1, this.args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            this.argsBuffer.SetData(this.args);
        }
        
        public void SetAnim(PoolList<int> allIndex,string animName)
        {
            int animIndex = -1;
            foreach (var animation in spriteAnimation.Clips)
            {
                animIndex++;
                if (animation.Name == animName)
                {
                    break;
                }
            }

            if (animIndex == -1)
            {
                Debug.LogError($"找不到名字为{animName}的动画");
                return;
            }

            foreach (var index in allIndex)
            {
                animIndexData[index - Order] = animIndex;
            }
        
            animIndexBuffer.SetData(animIndexData);
            computeShader.SetBuffer(kernel, "animIndexBuffer", animIndexBuffer);
        }
        
        private static Mesh CreateQuad() {
            // Just the same as previous code. I told you this can be refactored.
            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(0, 0, 0);
            vertices[1] = new Vector3(1, 0, 0);
            vertices[2] = new Vector3(0, 1, 0);
            vertices[3] = new Vector3(1, 1, 0);
            mesh.vertices = vertices;

            int[] tri = new int[6];
            tri[0] = 0;
            tri[1] = 2;
            tri[2] = 1;
            tri[3] = 2;
            tri[4] = 3;
            tri[5] = 1;
            mesh.triangles = tri;

            Vector3[] normals = new Vector3[4];
            normals[0] = -Vector3.forward;
            normals[1] = -Vector3.forward;
            normals[2] = -Vector3.forward;
            normals[3] = -Vector3.forward;
            mesh.normals = normals;

            Vector2[] uv = new Vector2[4];
            uv[0] = new Vector2(0, 0);
            uv[1] = new Vector2(1, 0);
            uv[2] = new Vector2(0, 1);
            uv[3] = new Vector2(1, 1);
            mesh.uv = uv;

            return mesh;
        }

        public void Dispose()
        {
            transformBuffer?.Dispose();
            uvBuffer?.Dispose();
            scaleBuffer?.Dispose();
            animIndexBuffer?.Dispose();
            stencilBuffer?.Dispose();
            argsBuffer?.Dispose();
        }
    }