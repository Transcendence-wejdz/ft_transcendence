// Same-origin path from ?redirectTo, falling back to the home page.
export function safeRedirect(url: URL): string {
    const target = url.searchParams.get('redirectTo');
    return target && target.startsWith('/') && !target.startsWith('//') ? target : '/';
}
