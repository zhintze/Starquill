shader_type canvas_item;

// Paper Texture Shader
// Creates a subtle procedural paper/parchment grain effect
// Use on ColorRect or Panel backgrounds for medieval scroll feel

// Uniforms for customization
uniform vec4 base_color : source_color = vec4(0.85, 0.75, 0.6, 1.0);
uniform vec4 tint_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float grain_intensity : hint_range(0.0, 1.0) = 0.15;
uniform float fiber_intensity : hint_range(0.0, 1.0) = 0.08;
uniform float vignette_intensity : hint_range(0.0, 1.0) = 0.1;
uniform float vignette_softness : hint_range(0.1, 2.0) = 0.8;
uniform float noise_scale : hint_range(1.0, 100.0) = 40.0;
uniform float fiber_scale : hint_range(1.0, 50.0) = 15.0;
uniform float seed : hint_range(0.0, 100.0) = 0.0;

// Simple hash function for noise
float hash(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

// Value noise
float noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);

	// Smoothstep for smoother interpolation
	vec2 u = f * f * (3.0 - 2.0 * f);

	// Four corners
	float a = hash(i);
	float b = hash(i + vec2(1.0, 0.0));
	float c = hash(i + vec2(0.0, 1.0));
	float d = hash(i + vec2(1.0, 1.0));

	// Bilinear interpolation
	return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

// Fractal Brownian Motion for more organic noise
float fbm(vec2 p, int octaves) {
	float value = 0.0;
	float amplitude = 0.5;
	float frequency = 1.0;

	for (int i = 0; i < octaves; i++) {
		value += amplitude * noise(p * frequency + seed);
		amplitude *= 0.5;
		frequency *= 2.0;
	}

	return value;
}

// Fiber-like noise (elongated in one direction)
float fiber_noise(vec2 p) {
	// Stretch in X direction for horizontal fiber look
	vec2 stretched = vec2(p.x * 0.3, p.y * 1.5);
	return fbm(stretched, 3);
}

void fragment() {
	vec2 uv = UV;

	// Base paper grain using FBM noise
	float grain = fbm(uv * noise_scale, 4);
	grain = (grain - 0.5) * 2.0; // Center around 0

	// Add fiber texture (subtle horizontal streaks)
	float fiber = fiber_noise(uv * fiber_scale);
	fiber = (fiber - 0.5) * 2.0;

	// Combine grain and fiber
	float texture_value = grain * grain_intensity + fiber * fiber_intensity;

	// Calculate vignette (darkened edges)
	vec2 center = uv - 0.5;
	float vignette = 1.0 - length(center) * vignette_intensity;
	vignette = smoothstep(0.0, vignette_softness, vignette);

	// Apply base color with tint
	vec3 paper_color = base_color.rgb * tint_color.rgb;

	// Add texture variation
	paper_color += vec3(texture_value);

	// Apply vignette (subtle edge darkening)
	paper_color *= mix(0.9, 1.0, vignette);

	// Clamp to valid range
	paper_color = clamp(paper_color, 0.0, 1.0);

	COLOR = vec4(paper_color, base_color.a * tint_color.a);
}
