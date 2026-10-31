<script lang="ts">
	import type { HTMLAttributes } from 'svelte/elements';

	type Props = {
		name: string;
		src?: string;
		team?: 'black' | 'blue' | 'purple' | 'red' | 'yellow';
		size?: 'sm' | 'md' | 'lg';
	} & HTMLAttributes<HTMLSpanElement>;

	let { src, name, team = 'blue', size = 'md', ...rest }: Props = $props();
	const initial = $derived(name.charAt(0).toUpperCase());
</script>

<span {...rest} aria-hidden="true" class="avatar team-{team} size-{size}">
	{#if src}
		<img {src} alt="" />
	{:else}
		{initial}
	{/if}
</span>

<style>
	.avatar {
		display: inline-grid;
		place-items: center;
		flex-shrink: 0;
		overflow: hidden;
		border: var(--border-width) solid var(--color-outline);
		border-radius: var(--radius);
		font-family: var(--font-ui);
		font-weight: 700;
		color: var(--color-outline);
	}

	.avatar img {
		width: 100%;
		height: 100%;
		object-fit: cover;
	}

	.size-sm {
		width: 1.875rem;
		height: 1.875rem;
		font-size: var(--text-sm);
	}

	.size-md {
		width: var(--control-sm);
		height: var(--control-sm);
		font-size: var(--text-lg);
	}

	.size-lg {
		width: 8rem;
		height: 8rem;
		font-size: 3rem;
	}

	.team-black {
		background: var(--color-team-black);
		color: var(--color-text);
	}

	.team-yellow {
		background: var(--color-team-yellow);
	}

	.team-blue {
		background: var(--color-team-blue);
	}

	.team-purple {
		background: var(--color-team-purple);
	}

	.team-red {
		background: var(--color-team-red);
	}
</style>
