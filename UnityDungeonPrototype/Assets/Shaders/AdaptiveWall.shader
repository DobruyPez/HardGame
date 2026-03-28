Shader "DungeonPrototype/AdaptiveWall"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _MetallicGlossMap ("Metallic", 2D) = "white" {}
        
        _Metallic ("Metallic", Range(0, 1)) = 0.3
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        
        // Lighting-based variation
        _DarkSmoothnessBoost ("Dark Area Smoothness Boost", Range(0, 1)) = 0.6
        _LightSmoothnessReduction ("Light Area Smoothness Reduction", Range(0, 1)) = 0.3
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        
        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _MetallicGlossMap;
        
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 worldPos;
            float3 worldNormal;
        };
        
        half _Metallic;
        half _Glossiness;
        half _DarkSmoothnessBoost;
        half _LightSmoothnessReduction;
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Get base albedo
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;
            
            // Get normal
            o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));
            
            // Calculate ambient light intensity at this position
            float3 worldNormal = normalize(IN.worldNormal);
            float ambientIntensity = length(ShadeSH9(half4(worldNormal, 1)));
            
            // Interpolate smoothness based on lighting
            // In darkness (low ambient): high smoothness (wet/slippery)
            // In light (high ambient): low smoothness (dry/old)
            float lightInfluence = saturate(ambientIntensity * 2); // Normalize to 0-1
            half smoothness = lerp(_Glossiness + _DarkSmoothnessBoost, 
                                   _Glossiness - _LightSmoothnessReduction, 
                                   lightInfluence);
            
            o.Smoothness = saturate(smoothness);
            o.Metallic = _Metallic;
        }
        ENDCG
    }
    
    FallBack "Standard"
}
