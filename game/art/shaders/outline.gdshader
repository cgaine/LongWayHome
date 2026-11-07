// Soft dark outline by an inverted hull (#102, ART_DIRECTION §6.1), the next pass of the farmer's body material only
// (FarmerModel), so no other model is outlined. The mesh is drawn again with its vertices pushed out along their
// normals by `width` metres and only its back faces kept (cull_front), unshaded in `colour`, so a thin dark rim shows
// round the silhouette. Skinning is applied before vertex(), so the hull follows the skeleton. One extra draw.
shader_type spatial;
render_mode unshaded, cull_front;

uniform float width = 0.012;
uniform vec4 colour : source_color = vec4(0.1647, 0.1333, 0.2, 1.0);

void vertex() {
	VERTEX += NORMAL * width;
}

void fragment() {
	ALBEDO = colour.rgb;
}
