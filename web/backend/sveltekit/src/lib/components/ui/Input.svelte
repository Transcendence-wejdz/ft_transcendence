<script lang="ts">
	import type { HTMLInputAttributes } from 'svelte/elements';

	type Props = {
		error?: string;
	} & HTMLInputAttributes;

	let { value = $bindable(), error, ...rest }: Props = $props();
	const id = $props.id();
</script>

<input bind:value {...rest} aria-invalid={error ? true : undefined} aria-describedby={error ? `${id}-error` : undefined} class="field"/>
{#if error}
	<span id="{id}-error" class="error">{error}</span>
{/if}

<style>
	.field {
		width: 100%;
		box-sizing: border-box;
		min-height: var(--control-md);
		padding: var(--space-sm) var(--space-md);
		font-family: var(--font-body);
		font-size: var(--text-md);
		color: var(--color-parchment-text);
		background: var(--color-input-bg);
		border: var(--border-width) solid var(--color-outline);
		border-radius: var(--radius);
		box-shadow: inset 0 3px 0 var(--color-parchment-shadow);
	}

	.field::placeholder {
		color: var(--color-input-placeholder);
	}

	.field:focus-visible {
		outline: 3px solid var(--color-focus);
		outline-offset: 2px;
	}

	.field[aria-invalid='true'] {
		border-color: var(--color-danger);
		box-shadow: 0 0 0 3px var(--color-danger);
	}

	.error {
		color: var(--color-danger-text);
		font-size: var(--text-sm);
	}

</style>
