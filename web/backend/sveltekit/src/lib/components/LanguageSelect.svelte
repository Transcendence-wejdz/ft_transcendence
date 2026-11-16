<script lang="ts">
	import { Select } from 'bits-ui';
	import { m } from '$lib/paraglide/messages.js';
	import { getLocale, setLocale, locales, type Locale } from '$lib/paraglide/runtime.js';

	const NAMES: Record<Locale, string> = { en: 'English', fr: 'Français', es: 'Español' };

	const current = getLocale();
</script>

<Select.Root type="single" value={current} onValueChange={(value) => setLocale(value as Locale)}>
	<Select.Trigger class="lang-trigger" aria-label={m.nav_language()}>
		<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="square" aria-hidden="true">
			<circle cx="12" cy="12" r="9" />
			<path d="M3 12h18M12 3c3 3.2 3 14.8 0 18M12 3c-3 3.2-3 14.8 0 18" />
		</svg>
		<span class="lang-code">{current.toUpperCase()}</span>
		<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="square" aria-hidden="true">
			<path d="M6 9l6 6 6-6" />
		</svg>
	</Select.Trigger>
	<Select.Portal disabled>
		<Select.Content class="lang-menu" align="end" sideOffset={8}>
			{#each locales as locale (locale)}
				<Select.Item class="lang-item" value={locale} label={NAMES[locale]}>
					{#snippet children({ selected })}
						<span lang={locale}>{NAMES[locale]}</span>
						{#if selected}
							<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3.2" stroke-linecap="square" aria-hidden="true">
								<path d="M5 12l5 5 9-10" />
							</svg>
						{/if}
					{/snippet}
				</Select.Item>
			{/each}
		</Select.Content>
	</Select.Portal>
</Select.Root>

<style>
	:global(.lang-trigger) {
		display: inline-flex;
		align-items: center;
		gap: var(--space-xs);
		height: var(--control-sm);
		padding: 0 0.625rem;
		border: var(--border-width) solid var(--color-outline);
		border-radius: var(--radius);
		background: var(--color-bg-raised);
		color: var(--color-text);
		box-shadow: inset 0 -4px 0 var(--color-bg-raised-shadow);
		cursor: pointer;
	}

	:global(.lang-trigger:hover) {
		background: var(--color-bg-raised-hover);
	}

	:global(.lang-trigger:focus-visible) {
		outline: 3px solid var(--color-focus);
		outline-offset: 3px;
	}

	.lang-code {
		font-family: var(--font-ui);
		font-weight: 700;
		font-size: 0.9375rem;
	}

	:global(.lang-menu) {
		min-width: 9rem;
		padding: var(--space-xs);
		background: var(--color-bg-card);
		border: var(--border-width) solid var(--color-outline);
		border-radius: var(--radius);
		box-shadow: var(--shadow-drop);
		z-index: 50;
	}

	:global(.lang-item) {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: var(--space-sm);
		min-height: var(--control-sm);
		padding: 0 0.75rem;
		border-radius: var(--radius);
		font-family: var(--font-ui);
		color: var(--color-text);
		cursor: pointer;
		outline: none;
	}

	:global(.lang-item[data-highlighted]) {
		background: var(--color-bg-raised);
	}
</style>
