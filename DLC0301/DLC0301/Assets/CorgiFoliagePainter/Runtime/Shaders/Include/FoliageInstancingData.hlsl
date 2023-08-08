#ifndef FOLIAGEINSTANCINGDATA
#define FOLIAGEINSTANCINGDATA

#if defined(SHADER_API_GLES3) || defined(SHADER_API_GLES) || defined(SHADER_API_GLCORE)
    #define CORGI_IS_WEBGL 1
#endif

#ifndef CORGI_IS_WEBGL
    // see: FoliageData.cs 
    struct FoliageMetadata
    {
        float4 color;
    };

    StructuredBuffer<FoliageMetadata> _FoliageMetadataBuffer;

    void GetFoliageColor_float(float4 uvData, float instance_index, out float4 color)
    {
#ifdef _USEBAKEDDATA
        color = float4(uvData.xyz, 1);
        instance_index = uvData.w;
        return;
#endif

        FoliageMetadata data = _FoliageMetadataBuffer[instance_index];
        color = data.color;
    };
#else
    Texture2D _FoliageMetadataFallbackTexture;
    SamplerState _SamplerLinearClamp;

    float4 _FoliageMetadataFallbackMetadata; // [group_size, 0, 0, 0]

    void GetFoliageColor_float(float4 uvData, float instance_index, out float4 color)
    {
#ifdef _USEBAKEDDATA
        color = uvData.xyz;
        instance_index = uvData.w;
        return;
#endif

        float groupSize = _FoliageMetadataFallbackMetadata.x;
        float uv_x = instance_index / groupSize;

        color = _FoliageMetadataFallbackTexture.Sample(_SamplerLinearClamp, float2(uv_x, 0));
    };
#endif

// note: due to shadergraph limitations, we are not actually using the trs matrix in our shadergraph shaders
// this section below is provided as convenience for writing your own procedural shaders outside of shadergraph 
// #if UNITY_ANY_INSTANCING_ENABLED
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    // see: FoliageData.cs 
    struct FoliageTransformationData
    {
        float4x4 trs;
        float4x4 inverseTrs;
    };

    StructuredBuffer<FoliageTransformationData> _FoliageTrsBuffer;

    void TransferLocalInputsToWorldOutputs(uint instanceID, float3 objectPosition, float3 objectNormal, float4 objectTangent, out float3 worldPosition, out float3 worldNormal, out float3 worldTangent, out float3 objectCenterPosition)
    {
        float4x4 trs = _FoliageTrsBuffer[instanceID].trs;

        worldPosition = mul(trs, float4(objectPosition, 1));
        worldNormal = normalize(mul(trs, float4(objectNormal, 0)).xyz);
        worldTangent = normalize(mul(trs, objectTangent).xyz);

        // worldNormal = normalize(worldNormal);
        // worldTangent = normalize(worldTangent);

        objectCenterPosition = mul(trs, float4(0, 0, 0, 1)); 
    }

    void foliageSetup() 
    {
        #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) && !defined(CORGI_IS_WEBGL)
            FoliageTransformationData data = _FoliageTrsBuffer[unity_InstanceID];
        
            unity_ObjectToWorld = data.trs;
            unity_WorldToObject = data.inverseTrs;

            // shadergraph wants this for HDRP, but then also complains... so. 
            // UNITY_MATRIX_M = data.trs;
            // UNITY_MATRIX_I_M  = data.inverseTrs;
        #endif
    }

    #define TRANSFER_FOLIAGE_TRS(i, objectPosition, objectNormal, objectTangent, worldPosition, worldNormal, worldTangent, objectCenterPosition) TransferLocalInputsToWorldOutputs(i, objectPosition, objectNormal, objectTangent, worldPosition, worldNormal, worldTangent, objectCenterPosition);
#else
    void TransferLocalInputsToWorldOutputs(float3 objectPosition, float3 objectNormal, float4 objectTangent, out float3 worldPosition, out float3 worldNormal, out float3 worldTangent, out float3 objectCenterPosition)
    {
        worldPosition = TransformObjectToWorld(objectPosition);
        worldNormal = TransformObjectToWorld(objectNormal);
        worldTangent = TransformObjectToWorld(objectTangent);

        objectCenterPosition = TransformObjectToWorld(float4(0, 0, 0, 1)); 
    }

    #define TRANSFER_FOLIAGE_TRS(i, objectPosition, objectNormal, objectTangent, worldPosition, worldNormal, worldTangent, objectCenterPosition) TransferLocalInputsToWorldOutputs(objectPosition, objectNormal, objectTangent, worldPosition, worldNormal, worldTangent, objectCenterPosition)
#endif

#endif