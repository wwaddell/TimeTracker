# TimeTracker Mobile (.NET MAUI)

Native Android + iOS app for the two things people do on the go: **logging time** and
**managing tasks**. All administration stays in the web app. The mobile app talks to the
same API (`https://time.wrdata.com`) and signs in with the same Entra External ID tenant.

## Architecture

- One MAUI project, `TimeTracker.Mobile`, targeting `net10.0-android` always and
  `net10.0-ios` only on macOS — so a Windows `dotnet build` does Android cleanly without a
  paired Mac.
- References `TimeTracker.Contracts` for the DTOs (same wire types as the web client).
- `AuthService` (MSAL.NET) → interactive Entra sign-in; token cached, refreshed silently.
  `AuthHttpMessageHandler` attaches the bearer token to every API call.
- `ApiClient` wraps the time-entry / task / project / me endpoints the app needs.
- `AppState` (singleton) holds the signed-in user, their orgs, and the sticky selected org.
- Pages: `LoginPage` → `AppShell` (bottom tabs: **Log Time**, **Tasks**), with
  add/edit pages pushed on top.

---

## One-time Entra setup (REQUIRED before sign-in works)

The mobile app reuses the existing **TimeTracker WASM** app registration (client id
`2ee4b766-35b8-4402-ab20-d15d0405c621`) but needs a **native redirect URI** added —
mobile apps can't use the SPA redirect.

1. Entra admin center → **App registrations → TimeTracker WASM → Authentication**.
2. **Add a platform → Mobile and desktop applications.**
3. Add this **custom redirect URI**:
   ```
   msal2ee4b766-35b8-4402-ab20-d15d0405c621://auth
   ```
4. (iOS, later) also add `msauth.com.wrdata.timetracker://auth`.
5. Save.

The Android `BrowserTabActivity` intent filter in `Platforms/Android/AndroidManifest.xml`
already matches the `msal2ee4b766-…` scheme. If the client id ever changes, update it in
three places: `AppConfig.ClientId`, the manifest `android:scheme`, and the Entra redirect.

---

## Building & running Android (Windows)

Prerequisites: the `android` workload (already installed) and an Android SDK + an emulator
or a USB-debugging device.

```powershell
# Restore + build just the Android head
dotnet build src/TimeTracker.Mobile/TimeTracker.Mobile.csproj -f net10.0-android

# List attached devices / emulators
dotnet build src/TimeTracker.Mobile/TimeTracker.Mobile.csproj -t:Run -f net10.0-android
```

In Visual Studio: set `TimeTracker.Mobile` as startup project, pick an emulator/device from
the run dropdown, and press F5.

First build pulls Android build-tools and is slow; subsequent builds are quick.

---

## Notes / future work

- **iOS**: build on a Mac (or Windows paired to a Mac). The iOS redirect + `OpenUrl` hook
  are already in place; add the iOS redirect URI to Entra (step 4 above) first.
- **Config fields**: the time-entry form covers note/date/duration/project/task. It does
  not yet render org-defined required configurable fields — entries needing those can be
  completed in the web app (they show as "incomplete" there until then).
- **Offline**: no offline cache yet; the app needs connectivity. A future enhancement
  could queue entries created offline.
