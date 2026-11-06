// Low-poly shading (#75), used through the shared palette material res://art/materials/palette.tres (#76).
// Colour: every pixel comes from the palette texture (ART_DIRECTION §8, one material on a palette texture). A
// code-built primitive sets the `swatch` instance uniform to its swatch's UV centre (ArtMaterials.Apply), so the whole
// mesh takes that colour; when `swatch` is unset (x < 0) the mesh's own UV samples the palette, which is how imported
// models UV-mapped onto palette.png get their colours. `tint` multiplies the result and is white by default.
// Style, the two ART_DIRECTION §9 still has to choose between, set project-wide by the global shader uniform
// `art_stepped_shading` ([shader_globals] in project.godot):
// - flat (false): one normal per face from screen-space derivatives, ordinary Lambert diffuse, so each face is a
//   single tone;
// - stepped (true): the mesh's smooth normals, with the diffuse cut into `steps` bands (highlight, lit and shade at
//   the default 3, ART_DIRECTION §6.1).
// The `style_override` instance uniform (-1 follow the global, 0 flat, 1 stepped) lets one mesh ignore the global;
// only the art sandbox uses it, to show both styles side by side.
// Both styles take every light's colour, energy and shadow (LIGHT_COLOR, ATTENUATION), and the environment's ambient
// light still applies (light() only replaces the per-light part), so they react to the sandbox's light presets.
// Matte: no specular.
shader_type spatial;

global uniform bool art_stepped_shading;

uniform sampler2D palette : source_color, filter_nearest;
uniform vec4 tint : source_color = vec4(1.0);
uniform int steps = 3;

instance uniform vec2 swatch = vec2(-1.0);
instance uniform int style_override = -1;

// Instance uniforms can't be read in light(), so the style is worked out once per vertex and passed on.
varying flat int v_stepped;

void vertex() {
	v_stepped = style_override < 0 ? (art_stepped_shading ? 1 : 0) : style_override;
}

void fragment() {
	ALBEDO = texture(palette, swatch.x >= 0.0 ? swatch : UV).rgb * tint.rgb;
	ROUGHNESS = 1.0;
	METALLIC = 0.0;
	if (v_stepped == 0) {
		// VERTEX is in view space here. The face normal's sign depends on the derivatives' screen orientation,
		// so turn it towards the camera (at the view-space origin): a visible face always faces the camera.
		vec3 face = normalize(cross(dFdx(VERTEX), dFdy(VERTEX)));
		NORMAL = dot(face, -VERTEX) < 0.0 ? -face : face;
	}
}

void light() {
	float lit = clamp(dot(NORMAL, LIGHT), 0.0, 1.0) * ATTENUATION;
	if (v_stepped != 0) {
		// Bands from 0 (shade: ambient only) to 1 (highlight); a cast shadow falls in the shade band.
		float bands = float(max(steps, 2));
		lit = min(floor(lit * bands), bands - 1.0) / (bands - 1.0);
	}
	// LIGHT_COLOR already includes the light's energy and a factor of PI, hence the division (Godot 4 docs).
	DIFFUSE_LIGHT += lit * LIGHT_COLOR / PI;
}
