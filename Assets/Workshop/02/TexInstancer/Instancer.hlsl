Texture2D<float4> tex;

float4x4 mat;
float3 position;

int rez;
float spacing;
float size;
float sizey;

PackedVaryingsType InstancedVert(AttributesMesh inputMesh, uint instanceID : SV_InstanceID)
{
	// Instance object space
    float4 pos = 0;
    pos.xyz = inputMesh.positionOS;
	
	// ID
    int3 id = 0;
    id.x = instanceID % rez;
    id.y = floor(instanceID / (float) rez);

	// Get color
    float4 c = tex[id.xy];
    c = clamp(c, 0, 1);
	
	// Height
    float height = sizey * (c.r * 1.5 + c.g * 1.7 + c.b * 1.3);
    pos.y *= height;
    pos.y += height / 2.0;
	
	#ifdef ATTRIBUTES_NEED_COLOR
		inputMesh.color = c;
	#endif

	// Grid Position
    float4 gpos = 0;
    gpos.xyz = id.xzy - rez / 2.0;
    gpos.y = 0;
    gpos *= spacing;
	
	// Size
    pos.xz *= size;

	// SET POSITION
    inputMesh.positionOS = mul(mat, pos + gpos).xyz;

    VaryingsType vt;
    vt.vmesh = VertMesh(inputMesh);
    return PackVaryingsType(vt);
}


void InstancedFrag(PackedVaryingsToPS packedInput, OUTPUT_GBUFFER(outGBuffer)
#ifdef _DEPTHOFFSET_ON
	, out float outputDepth : SV_Depth
#endif
)
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
	FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

	// input.positionSS is SV_Position
	PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

#ifdef VARYINGS_NEED_POSITION_WS
	float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
#else
	// Unused
	float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif

	SurfaceData surfaceData;
	BuiltinData builtinData;
	GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

	///////////////////////////////////////////////
	// Workshop Customize
	surfaceData.baseColor = input.color.rgb;
	///////////////////////////////////////////////

	ENCODE_INTO_GBUFFER(surfaceData, builtinData, posInput.positionSS, outGBuffer);

#ifdef _DEPTHOFFSET_ON
	outputDepth = posInput.deviceDepth;
#endif
}