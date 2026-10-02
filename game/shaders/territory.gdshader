// Toprak haritası: Sprite dokusu = arazi, owner_tex = tile sahibi (RG8, 16 bit),
// palette_tex = sahip ID → renk. Sınırlar komşu sahip farkından çizilir.
shader_type canvas_item;

uniform sampler2D owner_tex : filter_nearest;
uniform sampler2D palette_tex : filter_nearest;
uniform vec2 map_size;
uniform int local_owner = 0;
uniform int hover_owner = 0;

int owner_at(ivec2 p) {
	p = clamp(p, ivec2(0), ivec2(map_size) - 1);
	vec2 rg = texelFetch(owner_tex, p, 0).rg;
	return int(rg.r * 255.0 + 0.5) + int(rg.g * 255.0 + 0.5) * 256;
}

void fragment() {
	ivec2 p = ivec2(UV * map_size);
	vec3 terr = texture(TEXTURE, UV).rgb;
	int o = owner_at(p);
	vec3 col = terr;
	if (o != 0) {
		vec3 pc = texelFetch(palette_tex, ivec2(o % 1024, 0), 0).rgb;
		bool border = owner_at(p + ivec2(1, 0)) != o || owner_at(p - ivec2(1, 0)) != o
			|| owner_at(p + ivec2(0, 1)) != o || owner_at(p - ivec2(0, 1)) != o;
		if (border) {
			col = pc * 0.6;
			if (o == local_owner) col = mix(pc, vec3(1.0), 0.45 + 0.25 * sin(TIME * 4.0));
		} else {
			col = mix(terr, pc, 0.55);
			if (o == hover_owner) col = mix(col, vec3(1.0), 0.12);
		}
	}
	COLOR = vec4(col, 1.0);
}
