// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Blur" {
  Properties
  {
// ...
  }
 
  SubShader
  {
   Pass{
       // [Actual Shader]
       // ...
    }
   
   
   
    GrabPass { }
   
   
   
    Pass{   //1. Pass = Horizontal Blur
   
        Lighting Off
   
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 3.0
        #include "UnityCG.cginc"
       
        //Benutzervariablen
        float2 v_blurTexCoords[14];
        float2 v_texCoord;
       
        struct v2f{
            float4 pos : SV_POSITION;  
            float2  uv : TEXCOORD0;
        };
       
        v2f vert(appdata_base v){
            v2f o;
           
            o.pos = UnityObjectToClipPos (v.vertex);           //Convert Local-Space Position to Screen-Space Pos = Screen Space Pos. of Vertex
            o.uv = float4( v.texcoord.xy, 0, 0 );               //Texture Coordinate of Vertex (?) = a_texCoord
            o.uv = ComputeGrabScreenPos(o.pos);                 //OR this way?!
            v_texCoord = o.uv;
       
            v_blurTexCoords[ 0] = v_texCoord + float2(-0.028, 0.0);
            v_blurTexCoords[ 1] = v_texCoord + float2(-0.024, 0.0);
            v_blurTexCoords[ 2] = v_texCoord + float2(-0.020, 0.0);
            v_blurTexCoords[ 3] = v_texCoord + float2(-0.016, 0.0);
            v_blurTexCoords[ 4] = v_texCoord + float2(-0.012, 0.0);
            v_blurTexCoords[ 5] = v_texCoord + float2(-0.008, 0.0);
            v_blurTexCoords[ 6] = v_texCoord + float2(-0.004, 0.0);
            v_blurTexCoords[ 7] = v_texCoord + float2( 0.004, 0.0);
            v_blurTexCoords[ 8] = v_texCoord + float2( 0.008, 0.0);
            v_blurTexCoords[ 9] = v_texCoord + float2( 0.012, 0.0);
            v_blurTexCoords[10] = v_texCoord + float2( 0.016, 0.0);
            v_blurTexCoords[11] = v_texCoord + float2( 0.020, 0.0);
            v_blurTexCoords[12] = v_texCoord + float2( 0.024, 0.0);
            v_blurTexCoords[13] = v_texCoord + float2( 0.028, 0.0);
       
            return o;
        }
       
        sampler2D _GrabTexture;
       
        half4 frag (v2f i) : COLOR  {
            half4 texcol = (0.0);
           
            texcol += tex2D(_GrabTexture, v_blurTexCoords[ 0])*0.0044299121055113265;
            texcol += tex2D(_GrabTexture, v_blurTexCoords[ 1])*0.00895781211794;
            texcol += tex2D(_GrabTexture, v_blurTexCoords[ 2])*0.0215963866053;
            texcol += tex2D(_GrabTexture, v_blurTexCoords[ 3])*0.0443683338718;
            texcol += tex2D(_GrabTexture, v_blurTexCoords[ 4])*0.0776744219933;
            texcol += tex2D(_GrabTexture, v_blurTexCoords[ 5])*0.115876621105;
            texcol += tex2D(_GrabTexture, v_blurTexCoords[ 6])*0.147308056121;
            texcol += tex2D(_GrabTexture, v_texCoord         )*0.159576912161;
            texcol += tex2D(_GrabTexture, v_blurTexCoords[ 7])*0.147308056121;
            texcol += tex2D(_GrabTexture, v_blurTexCoords[ 8])*0.115876621105;
            texcol += tex2D(_GrabTexture, v_blurTexCoords[ 9])*0.0776744219933;
            texcol += tex2D(_GrabTexture, v_blurTexCoords[10])*0.0443683338718;
            texcol += tex2D(_GrabTexture, v_blurTexCoords[11])*0.0215963866053;
            texcol += tex2D(_GrabTexture, v_blurTexCoords[12])*0.00895781211794;
            texcol += tex2D(_GrabTexture, v_blurTexCoords[13])*0.0044299121055113265;
       
            return texcol;
        }
       
        ENDCG
    }
   
   Pass{    //2. Pass = Vertical Blur
    // ...
   }
  }
  Fallback "Diffuse"
}