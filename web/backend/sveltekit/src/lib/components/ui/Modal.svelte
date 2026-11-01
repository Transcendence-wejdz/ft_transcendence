<script lang="ts">
	import { AlertDialog } from 'bits-ui';
	import Button from '$lib/components/ui/Button.svelte';

	type Props = {
		open: boolean;
		title: string;
		description: string;
		confirmLabel: string;
		cancelLabel?: string;
		variant?: 'primary' | 'danger';
		onConfirm: () => void | Promise<void>;
	}

	let { open = $bindable(false), cancelLabel = 'Cancel', variant = 'danger',
		title, description, confirmLabel, onConfirm }: Props = $props();

	async function confirm() {
		await onConfirm();
		open = false;
	}
</script>

<AlertDialog.Root bind:open>
	<AlertDialog.Portal>
		<AlertDialog.Overlay class="modal-overlay" />
		<AlertDialog.Content class="modal">
			<AlertDialog.Title class="modal-title">{title}</AlertDialog.Title>
			<AlertDialog.Description class="modal-description">{description}</AlertDialog.Description>
			<div class="modal-actions">
				<AlertDialog.Cancel>
					{#snippet child({ props })}
						<Button variant="secondary" size="sm" {...props}>{cancelLabel}</Button>
					{/snippet}
				</AlertDialog.Cancel>
				<AlertDialog.Action>
					{#snippet child({ props })}
						<Button variant={variant} size="sm" {...props} onclick={confirm}>{confirmLabel}</Button>
					{/snippet}
				</AlertDialog.Action>
			</div>
		</AlertDialog.Content>
	</AlertDialog.Portal>
</AlertDialog.Root>

<style>
	:global(.modal-overlay) {
		position: fixed;
		inset: 0;
		z-index: 60;
		background: rgb(11 10 16 / 0.72 );
	}

	:global(.modal) {
		position: fixed;
		top: 50%;
		left: 50%;
		transform: translate(-50%, -50%);
		z-index: 61;
		width: calc(100% - 2rem);
		max-width: 28.75rem;
		padding: 1.75rem;
		background: var(--color-parchment);
		color: var(--color-parchment-text);
		border: var(--border-width) solid;
		border-radius: var(--radius-card);
		border-color: var(--color-outline);
		box-shadow: inset 0 0 0 4px var(--color-parchment-shadow), 0 6px 0 var(--color-drop);
	}

	:global(.modal-title) {
		margin: 0 0 var(--space-xs);
		font-family: var(--font-title);
		font-weight: 400;
		font-size: 2.375rem;
		line-height: 1;
	}

	:global(.modal-description) {
		margin: 0 0 1.5rem;
		color: var(--color-parchment-text-muted);
		line-height: 1.55;
	}

	.modal-actions {
		display: flex;
		flex-wrap: wrap;
		justify-content: flex-end;
		gap: var(--space-sm);
	}

</style>
