shader_type spatial;

void vertex() {
    float angle_normalized = acos(NORMAL.y) / (PI / 2.);
    
    if (angle_normalized < 0.4) {
        COLOR = vec4(0.2, 1, 0.3, 1);
    } else if (angle_normalized < 0.5) {
        COLOR = vec4(0.8, 0.5, 0.5, 1);
    } else {
        COLOR = vec4(0.4, 0.4, 0.5, 1);
    }
}

void fragment() {
    ALBEDO = COLOR.xyz;
}