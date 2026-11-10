<footer class="footer">
	<span>ft_transcendence · 42 · Art inspired by Tiny Swords</span>
	<nav class="nav" aria-label="Footer">
		<a href="/about">About</a>
		<a href="/privacy">Privacy Policy</a>
		<a href="/terms">Terms of Service</a>
	</nav>
</footer>

<style>
	.footer {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		justify-content: space-between;
		border-top: var(--border-width) solid var(--color-drop);
		background: var(--color-bg-bar);
		color: var(--color-text-muted);
		font-family: var(--font-ui);
		font-size: 0.9375rem;
		padding: 1.125rem clamp(1rem, 4vw, 3.5rem);
		gap: var(--space-xs) 1.5rem;
	}

	.nav {
		display: flex;
		flex-wrap: wrap;
		gap: 0.25rem 1.25rem;
	}

	.nav a {
		display: inline-flex;
		align-items: center;
		min-height: var(--control-sm);
		color: var(--color-text-secondary);
	}

	.nav a:hover {
		color: var(--color-link-hover);
	}
</style>
