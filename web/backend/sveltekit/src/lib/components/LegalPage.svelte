<script lang="ts">
	import type { Snippet } from 'svelte';
	import Ribbon from '$lib/components/ui/Ribbon.svelte';
	import Card from '$lib/components/ui/Card.svelte';

	type Section = { id: string; title: string; content: Snippet };

	type Props = {
		title: string;
		updated: string;
		sections: Section[];
		other: { href: string; label: string };
	};

	let { title, updated, sections, other }: Props = $props();
</script>

<svelte:head><title>Transcendence — {title}</title></svelte:head>

<Ribbon>Legal</Ribbon>
<h1>{title}</h1>
<p class="updated">Last updated {updated}</p>

<div class="layout">
	<nav class="toc" aria-label="Sections">
		{#each sections as section, i (section.id)}
			<a href="#{section.id}">{i + 1}. {section.title}</a>
		{/each}
		<a class="other" href={other.href}>{other.label} →</a>
	</nav>
	<div class="doc">
		<Card variant="parchment" as="article">
			{#each sections as section, i (section.id)}
				<h2 id={section.id}>{i + 1}. {section.title}</h2>
				{@render section.content()}
			{/each}
		</Card>
	</div>
</div>

<style>
	h1 {
		margin: 1.125rem 0 0.5rem;
		font-family: var(--font-title);
		font-weight: 400;
		font-size: clamp(3rem, 7vw, 4.5rem);
		line-height: 1;
	}

	.updated {
		margin: 0 0 2.5rem;
		color: var(--color-text-muted);
		font-family: var(--font-ui);
	}

	.layout {
		display: flex;
		flex-wrap: wrap;
		align-items: flex-start;
		gap: 2rem;
	}

	.toc {
		flex: 1 1 13.75rem;
		min-width: 0;
		display: flex;
		flex-direction: column;
		padding: 0.875rem;
		background: var(--color-bg-card);
		border: var(--border-width) solid var(--color-drop);
		border-radius: var(--radius-card);
	}

	.toc a {
		display: flex;
		align-items: center;
		min-height: 2.5rem;
		padding: 0 0.75rem;
		border-radius: var(--radius);
		color: var(--color-text-secondary);
		font-family: var(--font-ui);
		text-decoration: none;
	}

	.toc a:hover {
		color: #fff;
		background: var(--color-bg-raised);
	}

	.toc .other {
		margin-top: var(--space-xs);
		border-top: 2px solid var(--color-border-subtle);
		color: var(--color-gold);
	}

	.doc {
		flex: 999 1 35rem;
		min-width: 0;
	}

	h2 {
		margin: 2rem 0 0.625rem;
		font-family: var(--font-title);
		font-weight: 400;
		font-size: 2.125rem;
		line-height: 1.1;
	}

	h2:first-child {
		margin-top: 0;
	}

	.doc :global(p),
	.doc :global(li) {
		line-height: 1.7;
	}

	.doc :global(p) {
		margin: 0 0 0.75rem;
	}

	.doc :global(ul) {
		margin: 0 0 0.75rem;
		padding-left: 1.375rem;
	}

	.doc :global(a) {
		color: var(--color-ribbon);
	}

	@media (min-width: 900px) {
 	 .toc {
		position: sticky;
		top: 1.5rem;
		align-self: flex-start;
		max-height: calc(100vh - 3rem);
		overflow-y: auto;
		}
	}
</style>
