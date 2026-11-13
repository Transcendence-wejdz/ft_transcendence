<script lang="ts">
	type Props = { value: number | string };
	let { value }: Props = $props();

	// 5×7 glyphs, '#' = filled pixel
	const GLYPHS: Record<string, string[]> = {
		'0': ['.###.', '#...#', '#...#', '#...#', '#...#', '#...#', '.###.'],
		'1': ['..#..', '.##..', '..#..', '..#..', '..#..', '..#..', '.###.'],
		'2': ['.###.', '#...#', '....#', '...#.', '..#..', '.#...', '#####'],
		'3': ['####.', '....#', '....#', '.###.', '....#', '....#', '####.'],
		'4': ['...#.', '..##.', '.#.#.', '#..#.', '#####', '...#.', '...#.'],
		'5': ['#####', '#....', '#....', '####.', '....#', '....#', '####.'],
		'6': ['.###.', '#....', '#....', '####.', '#...#', '#...#', '.###.'],
		'7': ['#####', '....#', '...#.', '..#..', '..#..', '..#..', '..#..'],
		'8': ['.###.', '#...#', '#...#', '.###.', '#...#', '#...#', '.###.'],
		'9': ['.###.', '#...#', '#...#', '.####', '....#', '....#', '.###.']
	};

	const digits = $derived([...String(value)].filter((char) => char in GLYPHS));
	const path = $derived(
		digits
			.flatMap((digit, i) =>
				GLYPHS[digit].flatMap((row, y) =>
					[...row].flatMap((pixel, x) => (pixel === '#' ? [`M${i * 6 + x} ${y}h1v1h-1z`] : []))
				)
			)
			.join('')
	);
</script>

<svg class="pixel-number" viewBox="0 0 {digits.length * 6 - 1} 7" shape-rendering="crispEdges" aria-hidden="true">
	<path d={path} />
</svg>

<style>
	.pixel-number {
		display: block;
		width: clamp(13.75rem, 32vw, 23.75rem);
		height: auto;
		overflow: visible;
		fill: var(--color-gold);
		filter: drop-shadow(0 8px 0 var(--color-ribbon)) drop-shadow(0 6px 0 var(--color-drop));
	}
</style>
