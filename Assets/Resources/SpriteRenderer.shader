Shader "Instanced/ComputeBufferSprite" {
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
     
    SubShader {
        Tags{
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }
        Cull Back
        Lighting Off
        ZWrite On
        Blend One OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.5

            #if SHADER_TARGET >= 35 && (defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_XBOXONE) || defined(SHADER_API_PSSL) || defined(SHADER_API_SWITCH) || defined(SHADER_API_VULKAN) || (defined(SHADER_API_METAL) && defined(UNITY_COMPILER_HLSLCC)))
              #define SUPPORT_STRUCTUREDBUFFER
            #endif
 
			#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) && defined(SUPPORT_STRUCTUREDBUFFER)
               #define ENABLE_INSTANCING
            #endif
 
            #include "UnityCG.cginc"
 
            sampler2D _MainTex;
 
            // xy for position, z for rotation
            StructuredBuffer<float3> transformBuffer;
            StructuredBuffer<float> scaleBuffer;
            StructuredBuffer<bool> stencilBuffer;
 
            // xy is the uv size, zw is the uv offset/coordinate
           StructuredBuffer<fixed4> uvBuffer;
 
            struct v2f{
                float4 pos : SV_POSITION;
                float2 uv: TEXCOORD0;
                uint clip : UINT;
            };
 
            float4x4 rotationZMatrix(float zRotRadians) {
                float c = cos(zRotRadians);
                float s = sin(zRotRadians);
                float4x4 ZMatrix  = 
                    float4x4( 
                       c,  -s, 0,  0,
                       s,  c,  0,  0,
                       0,  0,  1,  0,
                       0,  0,  0,  1);
                return ZMatrix;
            }
 
            v2f vert (appdata_full v, uint instanceID : SV_InstanceID) {
                v2f o;
                if(stencilBuffer[instanceID] == 0)
                {
                    o.clip = 0;
                    return o;
                }
                float3 transform = transformBuffer[instanceID];
                fixed4 uv = uvBuffer[instanceID];
                //rotate the vertex
                v.vertex = mul(v.vertex - float4(0.5, 0.5, 0,0), rotationZMatrix(transform.z));
                 
                //scale it
                float3 worldPosition = float3(transform.x, transform.y, 0) + (v.vertex.xyz * scaleBuffer[instanceID]);
                 
                o.pos = UnityObjectToClipPos(float4(worldPosition, 1.0f));
                 
                // XY here is the dimension (width, height). 
                // ZW is the offset in the texture (the actual UV coordinates)
                o.uv = v.texcoord * uv.xy + uv.zw; 
                return o;
            }
 
            fixed4 frag (v2f i) : SV_Target{
                if(i.clip == 0)
                    clip(true);
                fixed4 col = tex2D(_MainTex, i.uv);
                clip(col.a - 0.01);
                col.rgb *= col.a;
                return col;
            }
 
            ENDCG
        }
    }
}