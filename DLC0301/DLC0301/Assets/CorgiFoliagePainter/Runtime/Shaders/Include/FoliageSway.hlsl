#ifndef FOLIAGESWAYINCLUDE
#define FOLIAGESWAYINCLUDE

#define WAVE_PI 3.1415926

// note: assumes the following paramaters exist
// you can use this macro in custom shaders 
#define DEFINE_WIND_SWAY_PARAMS float4 _Wave0;			\
								float4 _Wave1;			\
								float _WindSpeed;		\
								float _WindIntensity;	\
								float _WindScale;		\

float3 gerstnerWave(float4 wave, float3 pos, float windSpeed, float windIntensity)
{
	float2 d = normalize(wave.xy);

	float wavelength = wave.w;
	float steepness = wave.z;

	float t = _Time.y * windSpeed;
	float k = 2 * WAVE_PI / wavelength;
	float c = sqrt(9.8 / k);
	float f = k * (dot(d, pos.xz) - c * t);
	float a = steepness / k;

	return float3(
		d.x * (a * cos(f)),
		a * sin(f) * 0.1,
		d.y * (a * cos(f))
		) * windIntensity; 
}

void gestnerNormal(float4 wave, float3 pos, float windSpeed, float windIntensity, inout float3 tangent, inout float3 binormal)
{
	float2 d = normalize(wave.xy);

	float wavelength = wave.w;
	float steepness = wave.z;

	float t = _Time.y * windSpeed;
	float k = 2 * WAVE_PI / wavelength;
	float c = sqrt(9.8 / k);
	float f = k * (dot(d, pos.xz) - c * t);
	float a = steepness / k;

	tangent += float3(
		-d.x * d.x * (steepness * sin(f)),
		d.x * (steepness * cos(f)) * 0.1,
		-d.x * d.y * (steepness * sin(f))
		);

	binormal += float3( 
		-d.x * d.y * (steepness * sin(f)),
		d.y * (steepness * cos(f)) * 0.1,
		-d.y * d.y * (steepness * sin(f))
		);
}

void getNormalFromWaves(float4 wave0, float4 wave1, float windSpeed, float windIntensity, float windScale,
	float3 worldPos, float3 worldNormal, float3 worldTangent, out float3 normal, out float3 tangent)
{
	tangent = worldTangent;
	float3 binormal = cross(worldNormal, worldTangent);

	worldPos = worldPos * windScale;

	gestnerNormal(wave0, worldPos, windSpeed, windIntensity, tangent, binormal);
	gestnerNormal(wave1, worldPos, windSpeed, windIntensity, tangent, binormal);

	float3 targetNormal = normalize(cross(binormal, tangent));
	normal = lerp(worldNormal, targetNormal, windIntensity); 
}

float3 getWaveSway(float4 wave0, float4 wave1, float windSpeed, float windIntensity, float windScale, 
	float3 pos)
{
	pos = pos * windScale;
	float3 worldShift = 0;

	worldShift += gerstnerWave(wave0, pos, windSpeed, windIntensity);
	worldShift += gerstnerWave(wave1, pos, windSpeed, windIntensity);

	return worldShift;
}

void GetFoliageSway_float(float4 wave0, float4 wave1, float windSpeed, float windIntensity, float windScale, 
	float3 vertexPosition, float3 worldPosition, float3 worldNormal, float3 worldTangent, out float3 newWorldPosition, out float3 newWorldNormal, out float3 newWorldTangent)
{
	float intensity = vertexPosition.y;

	float3 gestnerPositionOffset = getWaveSway(wave0, wave1, windSpeed, windIntensity, windScale, worldPosition);

	float3 gestnerTangentOffset;
	float3 gestnerNormalOffset;
	getNormalFromWaves(wave0, wave1, windSpeed, windIntensity, windScale, worldPosition, worldNormal, worldTangent, gestnerNormalOffset, gestnerTangentOffset);

	newWorldPosition = worldPosition + gestnerPositionOffset * intensity;
	newWorldNormal = normalize(lerp(worldNormal, worldNormal + gestnerNormalOffset, intensity));
	newWorldTangent = normalize(lerp(worldTangent, worldTangent + gestnerTangentOffset, intensity));
};

#define DO_VERTEX_SWAY(vertexPosition, worldPosition, worldNormal, worldTangent, newWorldPosition, newWorldNormal, newWorldTangent) GetFoliageSway_float(_Wave0, _Wave1, _WindSpeed, _WindIntensity, _WindScale, vertexPosition, worldPosition, worldNormal, worldTangent.xyz, newWorldPosition, newWorldNormal, newWorldTangent);

#endif