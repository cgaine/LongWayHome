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
// Matte unless wet (#78): the `wet` instance uniform (0 dry, the default, to 1 soaked) lowers ROUGHNESS and raises
// SPECULAR, and since light() replaces Godot's own lighting it also adds a Blinn-Phong sheen to SPECULAR_LIGHT, scaled
// by `wet`. At wet = 0 the sheen term is zero, so every other mesh looks exactly as before. Only watered tile slabs and
// furrows set it (TileView).
// Night glow (#94): a merged mesh whose parts glow carries UV2 = (glow 0 to 1, twinkle 0 or 1) on every vertex
// (ArtMeshes.Merge). vertex() scales UV2.x by the global shader uniform `art_night` (0 by day to 1 at night,
// [shader_globals] in project.godot, set by NightGlowView from the clock and by the art sandbox's Night preset) and by
// a pulse: a slow breathing for a steady glow, out of step between neighbouring crops, or a sharp flash per speck for a
// twinkle. fragment() adds it as EMISSION in the part's own palette colour. A mesh with no UV2 reads it as zero, and
// at art_night = 0 the term is zero too, so every other mesh looks exactly as before. No bloom (REQ-14).
shader_type spatial;

global uniform bool art_stepped_shading;
global uniform float art_night;

uniform sampler2D palette : source_color, filter_nearest;
uniform vec4 tint : source_color = vec4(1.0);
uniform int steps = 3;

instance uniform vec2 swatch = vec2(-1.0);
instance uniform int style_override = -1;
instance uniform float wet = 0.0;

// How tight and how bright the wet sheen is (#78): a soft glint where the sun reflects, kept faint so watered soil
// stays darker than dry soil even facing the sun. The lower ROUGHNESS also gives wet soil a glossy sky reflection.
const float WET_SHININESS = 64.0;
const float WET_SHEEN = 0.06;

// How bright a full glow is at full night (#94), as a multiple of the part's palette colour.
const float GLOW_ENERGY = 1.6;

// Instance uniforms can't be read in light(), so the style and wetness are worked out once per vertex and passed on.
varying flat int v_stepped;
varying flat float v_wet;
// The night glow of this vertex (#94): UV2.x x art_night x the pulse, 0 on a mesh with no UV2.
varying float v_glow;

void vertex() {
	v_stepped = style_override < 0 ? (art_stepped_shading ? 1 : 0) : style_override;
	v_wet = wet;
	float pulse;
	if (UV2.y < 0.5) {
		// Steady: a slow breathing, offset by the crop's position so neighbours aren't in step (ART_DIRECTION §8).
		pulse = 0.85 + 0.15 * sin(TIME * 1.5 + NODE_POSITION_WORLD.x + NODE_POSITION_WORLD.z);
	} else {
		// Twinkle: a sharp flash, offset by the vertex's position so each speck flashes on its own.
		pulse = 0.35 + 0.65 * pow(0.5 + 0.5 * sin(TIME * 5.0 + dot(VERTEX, vec3(91.0, 37.0, 53.0))), 4.0);
	}
	v_glow = UV2.x * art_night * pulse;
}

void fragment() {
	ALBEDO = texture(palette, swatch.x >= 0.0 ? swatch : UV).rgb * tint.rgb;
	ROUGHNESS = mix(1.0, 0.25, wet);
	SPECULAR = mix(0.5, 0.7, wet);
	METALLIC = 0.0;
	EMISSION = ALBEDO * v_glow * GLOW_ENERGY;
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
	if (v_wet > 0.0) {
		// The wet sheen: Blinn-Phong, only where the surface faces the light.
		vec3 half_vector = normalize(LIGHT + VIEW);
		float sheen = pow(max(dot(NORMAL, half_vector), 0.0), WET_SHININESS) * step(0.0, dot(NORMAL, LIGHT));
		SPECULAR_LIGHT += v_wet * WET_SHEEN * sheen * ATTENUATION * LIGHT_COLOR / PI;
	}
}
