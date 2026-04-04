float4 _pos[1];

void SampleBuffer_float(float index, out float4 o) {
    o = _pos[0];
}

void SetFloat_float(in float4 p) {
    _pos[0] = p;
}
