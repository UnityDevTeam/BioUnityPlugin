Shader "Custom/AtomShader" 
{
	CGINCLUDE

	#include "UnityCG.cginc"
	
	struct AtomData
	{
	     float4 pos;
		 float4 info;
	};

	uniform int showAtomColors;

	uniform float scale;	
	uniform float highlightIntensity;

	uniform	StructuredBuffer<int> molStates;
	uniform	StructuredBuffer<int> molTypes;
	uniform	StructuredBuffer<float> atomRadii;
	uniform StructuredBuffer<float4> molColors;		
	uniform	StructuredBuffer<float4> atomColors;	
	uniform StructuredBuffer<AtomData> atomDataBuffer;
	
	struct vs2gs
	{
		float4 pos : SV_POSITION;
		float4 info: COLOR0;
					
	};
			
	struct gs2fs
	{
		float4 pos : SV_Position;									
		float4 info: COLOR0;	
		float4 info_2: COLOR1;	
		float2 uv: TEXCOORD0;						
	};

	float Epsilon = 1e-10;
 
 //*****//

	float3 RGBtoHCV(in float3 RGB)
	{
		// Based on work by Sam Hocevar and Emil Persson
		float4 P = (RGB.g < RGB.b) ? float4(RGB.bg, -1.0, 2.0/3.0) : float4(RGB.gb, 0.0, -1.0/3.0);
		float4 Q = (RGB.r < P.x) ? float4(P.xyw, RGB.r) : float4(RGB.r, P.yzx);
		float C = Q.x - min(Q.w, Q.y);
		float H = abs((Q.w - Q.y) / (6 * C + Epsilon) + Q.z);
		return float3(H, C, Q.x);
	}

	float3 HUEtoRGB(in float H)
	{
		float R = abs(H * 6 - 3) - 1;
		float G = 2 - abs(H * 6 - 2);
		float B = 2 - abs(H * 6 - 4);
		return saturate(float3(R,G,B));
	}

	//*****//

	float3 RGBtoHSL(in float3 RGB)
	{
		float3 HCV = RGBtoHCV(RGB);
		float L = HCV.z - HCV.y * 0.5;
		float S = HCV.y / (1 - abs(L * 2 - 1) + Epsilon);
		return float3(HCV.x, S, L);
	}

	float3 HSLtoRGB(in float3 HSL)
	{
		float3 RGB = HUEtoRGB(HSL.x);
		float C = (1 - abs(2 * HSL.z - 1)) * HSL.y;
		return (RGB - 0.5) * C + HSL.z;
	}

	//*****//

	float3 RGBtoHSV(in float3 RGB)
	{
		float3 HCV = RGBtoHCV(RGB);
		float S = HCV.y / (HCV.z + Epsilon);
		return float3(HCV.x, S, HCV.z);
	}

	float3 HSVtoRGB(in float3 HSV)
	{
		float3 RGB = HUEtoRGB(HSV.x);
		return ((RGB - 1) * HSV.y + 1) * HSV.z;
	}

	//*****//

	float3 ModifyHSV(float3 color, float3 hsv)
	{
		float3 c = RGBtoHSV(color);		
		
		//c.x = (hsv.x < 0) ? c.x : hsv.x;
		//c.y = (hsv.y < 0) ? c.y : hsv.y;
		//c.z = (hsv.z < 0) ? c.z : hsv.z;

		return 	HSVtoRGB(c + hsv);	
	}
	
	float3 ModifyHSL(float3 color, float3 hsl)
	{
		float3 c = RGBtoHSL(color);		
		
		//c.x = (hsl.x < 0) ? c.x : hsl.x;
		//c.y = (hsl.y < 0) ? c.y : hsl.y;
		//c.z = (hsl.z < 0) ? c.z : hsl.z;

		return 	HSLtoRGB(c + hsl);		
	}
	
	//*****//			

	vs2gs VS(uint id : SV_VertexID)
	{
		AtomData atomData = atomDataBuffer[id];				   
			    
		vs2gs output;				    		    
		output.pos = atomData.pos;
		output.info = atomData.info;        
		return output;
	}
			
	[maxvertexcount(4)]
	void GS(point vs2gs input[1], inout TriangleStream<gs2fs> triangleStream)
	{
		int molId = round(input[0].info.y);
		int atomId = round(input[0].info.x);
		
		int molType = molTypes[molId];
		int molState = molStates[molId];

		if( atomId <  0) return;
				
		float radius = scale * atomRadii[atomId];
		float4 pos = mul(UNITY_MATRIX_MVP, float4(input[0].pos.xyz, 1.0));
		float4 offset = mul(UNITY_MATRIX_P, float4(radius, radius, 0, 1));

		gs2fs output;					
		output.info = float4(radius, atomId, molId, molType); 	
		output.info_2 = float4(molState, 0, 0, 0); 	    

		//*****//

		output.uv = float2(1.0f, 1.0f);
		output.pos = pos + float4(output.uv * offset.xy, 0, 0);
		triangleStream.Append(output);

		output.uv = float2(1.0f, -1.0f);
		output.pos = pos + float4(output.uv * offset.xy, 0, 0);
		triangleStream.Append(output);	
								
		output.uv = float2(-1.0f, 1.0f);
		output.pos = pos + float4(output.uv * offset.xy, 0, 0);
		triangleStream.Append(output);

		output.uv = float2(-1.0f, -1.0f);
		output.pos = pos + float4(output.uv * offset.xy, 0, 0);
		triangleStream.Append(output);	
	}
			
	void FS (gs2fs input, out float4 color : COLOR0, out float4 normal_depth : COLOR1, out float4 id : COLOR2, out float depth : DEPTH) 
	{	
		float lensqr = dot(input.uv, input.uv);
    			
    	if(lensqr > 1.0) discard;					
		float3 normal = normalize(float3(input.uv, sqrt(1.0 - lensqr)));		
				
		// Find depth
		float eyeDepth = LinearEyeDepth(input.pos.z) + input.info.x * -normal.z ;
		depth = 1 / (eyeDepth * _ZBufferParams.z) - _ZBufferParams.w / _ZBufferParams.z;
		normal_depth = EncodeDepthNormal (depth, normal);
		
		// Find color								
		float ndotl = max( 0.0, dot(float3(0,0,1), normal));										
		float3 atomColor = (showAtomColors > 0 ) ? atomColors[round(input.info.y)].rgb : molColors[round(input.info.w)].rgb;
		
		//atomColor = ( round(input.info_2.x) < 2 ) ? atomColor :  ModifyHSL(atomColor, float3(-1, highlightIntensity, -1));
		atomColor = ( round(input.info_2.x) < 2 ) ? ModifyHSL(atomColor, float3(0, -0.025, -0.025)) :  ModifyHSL(atomColor, float3(0, highlightIntensity, highlightIntensity));

		float3 finalColor = atomColor * pow(ndotl, 0.075);				
		color = float4(finalColor, 1);
		
		// Set id
		uint t1 = input.info.z / 256;
		uint t2 = t1 / 256;
		float3 colorId = float3(t2 % 256, t1 % 256, input.info.z % 256) * 1/255;	
		id = float4(colorId, 1);					
	}		

	ENDCG
	
	SubShader 
	{			
		Pass
		{		
			ZWrite On
			BlendOp Max

			CGPROGRAM	
					
			#include "UnityCG.cginc"			
			
			#pragma vertex VS			
			#pragma fragment FS							
			#pragma geometry GS	
				
			#pragma only_renderers d3d11		
			#pragma target 5.0											
				
			ENDCG	
		}						
	}
	Fallback Off
}	