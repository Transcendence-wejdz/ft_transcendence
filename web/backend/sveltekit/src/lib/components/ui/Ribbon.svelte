<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { HTMLAttributes } from 'svelte/elements';

	type Props = {
		children: Snippet;
	} & HTMLAttributes<HTMLSpanElement>;

	let { children, ...rest }: Props = $props();
</script>

<span {...rest} class="ribbon">
	{@render children()}
</span>

<style>
	.ribbon {
		display: inline-block;
		padding: var(--space-xs) 2.125rem;
		background: var(--color-ribbon);
		color: var(--color-ribbon-text);
		font-family: var(--font-ui);
		font-weight: 700;
		font-size: var(--text-sm);
		letter-spacing: 0.14em;
		text-transform: uppercase;
		white-space: nowrap;
		clip-path: polygon(0 0, 100% 0, calc(100% - 14px) 50%, 100% 100%, 0 100%, 14px 50%);
	}
</style>
