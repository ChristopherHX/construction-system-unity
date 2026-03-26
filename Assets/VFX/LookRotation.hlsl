float3 LookRotation(in float3 d, in float3 axis) {
    // // float3 forward = normalize(d);
    // // float3 up = float3(0,1,0);

    // // float3 right = normalize(cross(up, forward));
    // // up = cross(forward, right);

    // // float3x3 rot = float3x3(right, up, forward);
    // // return rot * axis;
    // return axis;
    float3 forward = normalize(d);

    // Pick a helper vector that is not parallel to forward
    float3 helper = (abs(forward.y) < 0.999f) ? float3(0,1,0) : float3(1,0,0);

    // Build right and up
    float3 right = normalize(cross(helper, forward));
    float3 up    = cross(forward, right);

    if(axis.x == 1) {
        return right;
    }
    if(axis.y == 1) {
        return up;
    }
    if(axis.z == 1) {
        return forward;
    }
    return float3(0, 0, 0);
}
