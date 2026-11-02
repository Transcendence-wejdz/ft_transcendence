<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { HTMLAttributes } from 'svelte/elements';

	type Props = {
		dot?: string;
		children: Snippet;
	} & HTMLAttributes<HTMLSpanElement>;

	let { dot, children, ...rest }: Props = $props();
</script>

<span {...rest} class="tag">
	{#if dot}
		<span class="dot" aria-hidden="true" style:background={dot}></span>
	{/if}
	{@render children()}
</span>

<style>
	.tag {
		display: inline-flex;
		align-items: center;
		gap: var(--space-xs);
		padding: 0.3125rem 0.75rem;
		border: 2px solid var(--color-outline);
		border-radius: var(--radius-pill);
		font-family: var(--font-ui);
		font-size: var(--text-sm);
		background: var(--color-input-bg);
		color: var(--color-parchment-text);
	}

	.dot {
		width: 12px;
		height: 12px;
		border-radius: 50%;
		border: 2px solid var(--color-outline);
	}
</style>
