#ifndef FOLIAGEDISPLACEMENTINCLUDED
#define FOLIAGEDISPLACEMENTINCLUDED

#define DEFINE_FOLIAGE_DISPLACEMENT_PARAMS float _FoliageDisplacementAmount;

#include "FoliageInstancingData.hlsl"

#ifndef CORGI_IS_WEBGL
    struct FoliageDisplacementData
    {
        float3 position;
        float radius;
    };

    StructuredBuffer<FoliageDisplacementData> _FoliageDisplacementBuffer;
    float _FoliageDisplacementCount;

    void GetFoliageVertexDisplacement_float(float foliageDisplacementAmount, float3 objectCenterPosition, float3 vertexPosition, float3 worldPosition, out float3 newWorldPosition, out float offsetDistance)
    {
        float3 offset = float3(0.0, 0.0, 0.0);

        for (int i = 0; i < _FoliageDisplacementCount; ++i)
        {
            FoliageDisplacementData displacementData = _FoliageDisplacementBuffer[i];

            float3 toDisplacer = (displacementData.position - objectCenterPosition);

            float distanceToDisplacer = max(0, length(toDisplacer) - displacementData.radius);
            float offsetAmount = saturate(1.0 - distanceToDisplacer - 0.05); 
        
            toDisplacer.y = 0; // flatten 
            float3 displaceDirection = normalize(-toDisplacer) + float3(0, -0.5, 0);

            offset += displaceDirection * offsetAmount; 
        }

        float distanceFromObjectCenter = vertexPosition.y;
        float totalOffsetAmount = distanceFromObjectCenter;

        newWorldPosition = worldPosition + offset * totalOffsetAmount * foliageDisplacementAmount;
        offsetDistance = length(offset * totalOffsetAmount * foliageDisplacementAmount);
    }

    #define DO_VERTEX_DISPLACEMENT(objectCenterPosition, vertexPosition, worldPosition, newWorldPosition, offsetDistance) GetFoliageVertexDisplacement_float(_FoliageDisplacementAmount, objectCenterPosition, vertexPosition, worldPosition, newWorldPosition, offsetDistance);
#else
    void GetFoliageVertexDisplacement_float(float foliageDisplacementAmount, float3 objectCenterPosition, float3 vertexPosition, float3 worldPosition, out float3 newWorldPosition, out float offsetDistance)
    {
        newWorldPosition = worldPosition;
        offsetDistance = 0.0;
    }

    #define DO_VERTEX_DISPLACEMENT(objectCenterPosition, vertexPosition, worldPosition, newWorldPosition, offsetDistance) float3 newWorldPosition = worldPosition; float offsetDistance = 0;
#endif

#endif