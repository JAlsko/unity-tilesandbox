    Shader "GUI/OutlineText" {
    Properties {
        _MainTex ("Font Texture", 2D) = "white" {}
        _BumpMap ("Base (RGB)", 2D) = "white" {}
     
        _Color ("Text Color", Color) = (1,1,1,1)
    }
     
    SubShader {
        Tags {"Queue" = "Transparent"}
        Lighting Off Cull Off ZTest Always ZWrite Off Fog { Mode Off }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            Color [_Color]
            SetTexture [_MainTex] {
                combine primary, texture * primary
            }
            SetTexture [_BumpMap] {
                combine texture * previous, previous * texture
            }
        }
    }
    }
