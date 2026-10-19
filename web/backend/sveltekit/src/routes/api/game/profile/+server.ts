import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import type { Prisma } from '$lib/server/generated/prisma/client';
import { prisma } from '$lib/server/db.js';
import { CLASSES, COLORS, displayNameError, isUniqueViolation, parseGameSettings } from '$lib/server/validation.js';

// Partial profile/settings update pushed by the Unity game whenever the player changes something in-game.
export const PATCH: RequestHandler = async ({ locals, request }) => {
    if (!locals.user) return json({ success: false, error: 'Not authenticated' }, { status: 401 });

    let body: unknown;
    try {
        body = await request.json();
    } catch {
        return json({ success: false, error: 'Invalid request body' }, { status: 400 });
    }
    if (!body || typeof body !== 'object' || Array.isArray(body)) {
        return json({ success: false, error: 'Invalid request body' }, { status: 400 });
    }

    const input = body as Record<string, unknown>;
    const data: Prisma.UserUpdateInput = {};

    if (input.display_name !== undefined) {
        const displayName = typeof input.display_name === 'string' ? input.display_name.trim() : '';
        const nameError = displayNameError(displayName);
        if (nameError) return json({ success: false, error: nameError }, { status: 400 });
        data.displayName = displayName;
    }
    if (input.class !== undefined) {
        if (typeof input.class !== 'string' || !CLASSES.has(input.class)) {
            return json({ success: false, error: 'Unknown class.' }, { status: 400 });
        }
        data.class = input.class;
    }
    if (input.color !== undefined) {
        if (typeof input.color !== 'string' || !COLORS.has(input.color)) {
            return json({ success: false, error: 'Unknown color.' }, { status: 400 });
        }
        data.color = input.color;
    }
    if (input.settings !== undefined) {
        const settings = parseGameSettings(input.settings);
        if (!settings) return json({ success: false, error: 'Invalid game settings.' }, { status: 400 });
        const current = locals.user.game_settings;
        const base = current && typeof current === 'object' && !Array.isArray(current) ? current : {};
        data.gameSettings = { ...base, ...settings };
    }
    if (Object.keys(data).length === 0) {
        return json({ success: false, error: 'Nothing to update' }, { status: 400 });
    }

    try {
        const user = await prisma.user.update({
            where: { id: locals.user.id },
            data,
            select: { displayName: true, color: true, class: true, gameSettings: true }
        });
        return json({
            success: true,
            user: {
                display_name: user.displayName,
                color: user.color,
                class: user.class,
                game_settings: user.gameSettings
            }
        });
    } catch (error) {
        if (isUniqueViolation(error)) {
            return json({ success: false, error: 'That player name is already taken.' }, { status: 409 });
        }
        console.error('Failed to save in-game profile:', error);
        return json({ success: false, error: 'Could not save your profile.' }, { status: 500 });
    }
};
