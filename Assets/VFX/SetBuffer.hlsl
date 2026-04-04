float4 _pos[1];

void SetFloat_float(VFXAttributes attributes, in float4 p) {
    _pos[0] = p;
}
