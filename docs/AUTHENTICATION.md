# Authentication

The app uses provider login only to prove identity. After email/password, Google, or Facebook login succeeds, the backend creates or loads a local `User` and issues its own signed JWT app token. Protected API calls use that app token, not Google or Facebook tokens.

## Architecture

- Backend auth service verifies credentials and external provider tokens.
- Backend token service signs short-lived JWTs with `Authentication:Jwt` settings.
- Frontend stores the returned app token and sends it as `Authorization: Bearer <token>`.
- Protected controllers use ASP.NET Core JWT bearer authentication.
- Proposal-flow endpoints derive the current user id from JWT claims, never from request bodies or query strings.

No database migration is required for the current implementation. App sessions are stateless JWTs; refresh tokens are not implemented yet.

## Login Flow

1. User submits email/password, Google, or Facebook login in Flutter.
2. Flutter sends the provider credential to the backend login endpoint.
3. Backend validates the credential.
4. Backend creates or loads the local `User`.
5. Backend returns:
   - `accessToken`
   - `expiresAtUtc`
   - `tokenType`
   - `user`
6. Flutter saves the token and expiration.
7. Flutter considers the user logged in only while it has a local user session and a non-expired app token.

## JWT Lifecycle

JWT settings live under `Authentication:Jwt`:

- `Issuer`
- `Audience`
- `SigningKey`
- `ExpirationMinutes`

`JWT_SIGNING_KEY` can override the configured signing key in environment-specific deployments. Production should use a strong secret from environment or secret storage, not the development key in `appsettings.json`.

JWTs include:

- `sub`: local user id
- `ClaimTypes.NameIdentifier`: local user id
- `email`: user email where available
- `unique_name`: username where available
- `jti`: unique token id

## Token Expiration

The backend validates token lifetime on every protected request. Expired, malformed, or incorrectly signed tokens return `401 Unauthorized`.

The frontend also stores `expiresAtUtc`. Before attaching a token, it checks local expiration. If the token is expired, the frontend clears it and notifies the auth controller so the UI returns to login.

## Logout Flow

Logout is local and provider-aware:

- For Google users, Flutter signs out of Google.
- For Facebook users, Flutter logs out of Facebook.
- Flutter clears the stored app token.
- Flutter clears the local session and returns to login.

Because the current app token is stateless, logout does not revoke already-issued JWTs server-side. Server-side revocation can be added with refresh-token/session storage later.

## Google Authentication Flow

Flutter obtains a Google ID token from `google_sign_in` and sends it to `/google-login`.

The backend validates the token with Google's token info endpoint and checks:

- token is valid,
- email exists and is verified,
- audience matches `Authentication:GoogleClientId` when configured.

The backend stores Google's provider subject as the local user's Google identifier, then issues the app JWT.

## Facebook Authentication Flow

Flutter obtains a Facebook access token from `flutter_facebook_auth` and sends it to `/fb-login`.

The backend validates the token by calling Facebook Graph API `/me`. If `Authentication:FacebookAppAccessToken` is configured, it also calls `debug_token` and checks:

- token is valid,
- token belongs to the expected user,
- app id matches `Authentication:FacebookAppId` when configured.

The backend stores Facebook's provider user id as the local user's Facebook identifier, then issues the app JWT.

## Protected Endpoints

These endpoints require `Authorization: Bearer <app-token>`:

- `GET /user`
- `PUT /user`
- `DELETE /user/{id}`
- `PUT /vote`
- `POST /vote`
- all `/proposal` endpoints
- all `/party` endpoints
- all `/parliament-import` endpoints
- `POST /proposal-flow/feed`
- `POST /proposal-flow/interactions`
- `GET /proposal-flow/initiatives/{initiativeId}/reveal`
- `GET /proposal-flow/initiatives/{initiativeId}/journey`

These endpoints are intentionally anonymous:

- `GET /`
- `POST /user`
- `POST /user-login`
- `POST /google-login`
- `POST /fb-login`
- `GET /healthz`

## Frontend Token Handling

Flutter stores the app token through `AppTokenStore`, backed by `flutter_secure_storage`.

`ApiClient` asks `AppTokenStore` for the current token before each request. If present and not expired, the client adds:

```text
Authorization: Bearer <token>
```

If any API response is `401`, `ApiClient` calls the global unauthorized handler. `AuthController` handles that by clearing the local session, which causes the app shell to render the login screen.

## Storage Tradeoffs

`localStorage` is simple for Flutter web and survives refreshes, but JavaScript can read it. If the app has an XSS issue, tokens in `localStorage` are exposed.

Secure storage is the best pragmatic mobile/desktop option because iOS Keychain, Android Keystore-backed storage, and desktop secure stores reduce accidental token exposure. `flutter_secure_storage` also supports web, though browser storage still depends on web platform constraints.

HttpOnly cookies are strong for browser-only apps because JavaScript cannot read them. They also enable server-managed sessions and refresh-token rotation. They are less convenient for Flutter mobile because mobile clients need cookie handling and CSRF/CORS setup designed around browser semantics.

Recommended option right now: use `flutter_secure_storage` for the app token. It works across Flutter web/mobile/desktop with one app code path and keeps mobile security sensible. Revisit HttpOnly cookies if the product becomes primarily browser-based.

## Future Refresh-Token Strategy

The next session-hardening step should add refresh tokens:

1. Backend issues short-lived access tokens and longer-lived refresh tokens.
2. Refresh tokens are stored hashed in the database with user id, expiry, revocation timestamp, and rotation metadata.
3. Frontend uses the access token for API calls.
4. When the access token expires, frontend calls a refresh endpoint.
5. Backend rotates the refresh token and returns a new access token.
6. Logout revokes the active refresh token.
7. Suspected reuse revokes the token family.

For web, refresh tokens should ideally be HttpOnly, Secure, SameSite cookies. For mobile, store refresh tokens in secure storage.
