//  StructuredBuffer buffer

float CustomSampleCurve(VFXCurve curve, float time)
{
    // saturate clamp between 0 and 1
    float eps = 0.0001;
    float a = saturate(time - eps);
    float b = saturate(time + eps);
	return (b - a) / (SampleCurve(curve, b) - SampleCurve(curve, a));
}
