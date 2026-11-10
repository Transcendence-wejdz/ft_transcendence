import { redirect, type Handle } from '@sveltejs/kit';
import { SESSION_COOKIE, validateSession } from '$lib/server/session.js';

export const handle: Handle = async ({ event, resolve }) => {
    const token = event.cookies.get(SESSION_COOKIE);
    event.locals.user = token ? await validateSession(token) : null;

    if (token && !event.locals.user) {
        event.cookies.delete(SESSION_COOKIE, { path: '/' }); // stale cookie
    }
	//this if covers actions, +server.ts and page load in (app)/, which the layout load alone does not
    if (event.route.id?.startsWith('/(app)/') && !event.locals.user) {
        redirect(303, `/login?redirectTo=${encodeURIComponent(event.url.pathname + event.url.search)}`);
    }
    return resolve(event);
};
