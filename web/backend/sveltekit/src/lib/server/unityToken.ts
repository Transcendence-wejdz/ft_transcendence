import { createPublicKey, verify, type KeyObject } from 'node:crypto';

const JWKS_URL = 'https://player-auth.services.api.unity.com/.well-known/jwks.json';
const ISSUER = 'https://player-auth.services.api.unity.com';
const EXPECTED_ALGORITHM = 'RS256';
const CLOCK_SKEW_SECONDS = 60;

type TokenKeys = Map<string, KeyObject>;
type KeyLookup = (kid: string) => Promise<KeyObject | undefined>;

let cachedKeys: TokenKeys = new Map();

function decodeJson(part: string): Record<string, unknown> | null {
    try {
        const value: unknown = JSON.parse(Buffer.from(part, 'base64url').toString('utf8'));
        return value && typeof value === 'object' && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
    } catch {
        return null;
    }
}

function areClaimsValid(claims: Record<string, unknown>, nowSeconds: number): boolean {
    if (claims.iss !== ISSUER || typeof claims.exp !== 'number') return false;
    if (claims.exp + CLOCK_SKEW_SECONDS < nowSeconds) return false;
    return typeof claims.nbf !== 'number' || claims.nbf - CLOCK_SKEW_SECONDS <= nowSeconds;
}

// The token's player id once its signature, issuer and dates all check out, else null; the algorithm is pinned so a token can't choose its own.
export async function verifiedSubject(token: string, keyFor: KeyLookup, nowSeconds: number): Promise<string | null> {
    const parts = token.split('.');
    if (parts.length !== 3) return null;

    const header = decodeJson(parts[0]);
    const claims = decodeJson(parts[1]);
    if (!header || !claims || header.alg !== EXPECTED_ALGORITHM || typeof header.kid !== 'string') return null;

    const key = await keyFor(header.kid);
    if (!key) return null;
    if (!verify('RSA-SHA256', Buffer.from(`${parts[0]}.${parts[1]}`), key, Buffer.from(parts[2], 'base64url'))) return null;

    return areClaimsValid(claims, nowSeconds) && typeof claims.sub === 'string' ? claims.sub : null;
}

async function fetchKeys(): Promise<TokenKeys> {
    const response = await fetch(JWKS_URL);
    if (!response.ok) throw new Error(`Unity JWKS answered ${response.status}`);

    const body = (await response.json()) as { keys?: Array<{ kid?: string; kty?: string; n?: string; e?: string }> };
    const keys: TokenKeys = new Map();
    for (const jwk of body.keys ?? []) {
        if (jwk.kid && jwk.kty === 'RSA' && jwk.n && jwk.e) {
            keys.set(jwk.kid, createPublicKey({ key: { kty: 'RSA', n: jwk.n, e: jwk.e }, format: 'jwk' }));
        }
    }
    return keys;
}

// Unity rotates its keys, so an unknown key id refreshes the cache once instead of being refused outright.
async function unityKeyFor(kid: string): Promise<KeyObject | undefined> {
    if (!cachedKeys.has(kid)) cachedKeys = await fetchKeys();
    return cachedKeys.get(kid);
}

// The Unity player this access token really belongs to, or null when it isn't a valid token; throws if Unity's keys can't be reached.
export function unityPlayerIdFromToken(token: string): Promise<string | null> {
    return verifiedSubject(token, unityKeyFor, Date.now() / 1000);
}
