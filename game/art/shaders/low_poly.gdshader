// Low-poly shading for the art sandbox (#75), with the two styles ART_DIRECTION §9 still has to choose between:
// - flat (stepped = false): one normal per face from screen-space derivatives, ordinary Lambert diffuse, so each
//   face is a single tone;
// - stepped (stepped = true): the mesh's smooth normals, with the diffuse cut into `steps` bands (highlight, lit and
//   shade at the default 3, ART_DIRECTION §6.1).
// Both take every light's colour, energy and shadow (LIGHT_COLOR, ATTENUATION), and the environment's ambient light
// still applies (light() only replaces the per-light part), so they react to the sandbox's light presets. Matte: no
// specular.
shader_type spatial;

uniform vec4 albedo : source_color = vec4(1.0);
uniform bool stepped = false;
uniform int steps = 3;

void fragment() {
	ALBEDO = albedo.rgb;
	ROUGHNESS = 1.0;
	METALLIC = 0.0;
	if (!stepped) {
		// VERTEX is in view space here. The face normal's sign depends on the derivatives' screen orientation,
		// so turn it towards the camera (at the view-space origin): a visible face always faces the camera.
		vec3 face = normalize(cross(dFdx(VERTEX), dFdy(VERTEX)));
		NORMAL = dot(face, -VERTEX) < 0.0 ? -face : face;
	}
}

void light() {
	float lit = clamp(dot(NORMAL, LIGHT), 0.0, 1.0) * ATTENUATION;
	if (stepped) {
		// Bands from 0 (shade: ambient only) to 1 (highlight); a cast shadow falls in the shade band.
		float bands = float(max(steps, 2));
		lit = min(floor(lit * bands), bands - 1.0) / (bands - 1.0);
	}
	// LIGHT_COLOR already includes the light's energy and a factor of PI, hence the division (Godot 4 docs).
	DIFFUSE_LIGHT += lit * LIGHT_COLOR / PI;
}
