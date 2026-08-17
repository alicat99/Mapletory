Shader "Maptory/ConstructionGridOverlay"
{
    Properties
    {
        _LineColor ("Line Color", Color) = (0, 0, 0, 0.18)
        _LineWidth ("Line Width (Pixels)", Range(0.5, 2)) = 0.75
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "UnityCG.cginc"

            struct VertexInput
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 vertex : SV_POSITION;
                float2 grid_position : TEXCOORD0;
            };

            fixed4 _LineColor;
            float _LineWidth;

            VertexOutput Vertex(VertexInput input)
            {
                VertexOutput output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.grid_position = input.uv;
                return output;
            }

            fixed4 Fragment(VertexOutput input) : SV_Target
            {
                float2 pixel_size = max(fwidth(input.grid_position), 0.000001);
                float2 edge_distance = min(
                    frac(input.grid_position),
                    1.0 - frac(input.grid_position));
                float2 line_mask = 1.0 - smoothstep(
                    pixel_size * (_LineWidth - 0.5),
                    pixel_size * (_LineWidth + 0.5),
                    edge_distance);
                fixed4 color = _LineColor;
                color.a *= max(line_mask.x, line_mask.y);
                return color;
            }
            ENDCG
        }
    }
}
