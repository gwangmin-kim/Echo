Shader "Echo/Sound Reactive Surface"
{
    Properties
    {
        [HDR] _SoundWaveColor ("Response Color", Color) = (1, 1, 1, 1)
        _SoundWaveBrightness ("Brightness", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "SoundWaveSurface"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define MAX_SOUND_WAVES 16

            CBUFFER_START(UnityPerMaterial)
                half4 _SoundWaveColor;
                half _SoundWaveBrightness;
            CBUFFER_END

            int _SoundWaveCount;
            float4 _SoundWaveOrigins[MAX_SOUND_WAVES];
            float4 _SoundWaveParams[MAX_SOUND_WAVES];
            float _SoundWaveTime;
            float _SoundWavePropagationSpeed;

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float response = 0;

                [unroll]
                for (int i = 0; i < MAX_SOUND_WAVES; i++)
                {
                    if (i >= _SoundWaveCount)
                        break;

                    float3 origin = _SoundWaveOrigins[i].xyz;
                    float startTime = _SoundWaveOrigins[i].w;
                    float maxRadius = max(_SoundWaveParams[i].x, 0.001);
                    float thickness = max(_SoundWaveParams[i].y, 0.001);
                    float traceDuration = max(_SoundWaveParams[i].z, 0.001);
                    float intensity = max(_SoundWaveParams[i].w, 0);

                    float elapsed = max(0, _SoundWaveTime - startTime);
                    float radius = elapsed * max(_SoundWavePropagationSpeed, 0.001);
                    float surfaceDistance = distance(input.positionWS, origin);
                    float distanceToFront = abs(surfaceDistance - radius);

                    float front = 1 - smoothstep(thickness * 0.35, thickness, distanceToFront);
                    float arrivalAge = elapsed - surfaceDistance / max(_SoundWavePropagationSpeed, 0.001);
                    float trace = step(0, arrivalAge) * (1 - saturate(arrivalAge / traceDuration));
                    float distanceFalloff = 1 - saturate(surfaceDistance / maxRadius);

                    response = max(response, max(front, trace * 0.35) * distanceFalloff * intensity);
                }

                half3 emission = _SoundWaveColor.rgb * (_SoundWaveBrightness * response);
                return half4(emission, 1);
            }
            ENDHLSL
        }
    }
}
