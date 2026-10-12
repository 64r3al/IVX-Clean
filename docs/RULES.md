# IVX Clean — Cleaning rules

> This file is generated from the rule catalog by `tools/RulesDocGen`. Do not edit by hand.

IVX Clean cleans **only** the locations listed below. Every rule is an explicit, reviewed
entry; if a location is not covered by a rule, the cleaner cannot touch it. Each item below
also passes the SafetyGuard, which refuses protected system and personal locations, reparse
points, and protected file types — even when a rule would otherwise match.

**37 rules** across four levels.

## Level 1 — Quick

### Temporary files (Windows)  `windows.temp.machine`

- **Category:** Temporary files
- **Risk:** Safe · **Action:** Delete
- **Why:** The machine-wide temporary folder used by Windows and installers. Rebuilt as needed.
- **Locations:**
  - `{Windows}\Temp`

### Temporary files (your account)  `windows.temp.user`

- **Category:** Temporary files
- **Risk:** Safe · **Action:** Delete
- **Why:** The per-user temporary folder. Apps and Windows write scratch files here and recreate them on demand. Files touched in the last 24 hours are left alone in case they are still in use.
- **Locations:**
  - `{Temp}`

### Crash dumps  `windows.crashdumps.user`

- **Category:** Windows
- **Risk:** Safe · **Action:** Delete
- **Why:** Application crash dump files written here by Windows Error Reporting. Only useful while actively debugging a crash.
- **Locations:**
  - `{LocalAppData}\CrashDumps`

### Thumbnail and icon cache  `windows.thumbnailcache`

- **Category:** Windows
- **Risk:** Safe · **Action:** Delete
- **Why:** Explorer's thumbnail and icon caches. Windows rebuilds them automatically the next time you browse folders.
- **Locations:**
  - `{LocalAppData}\Microsoft\Windows\Explorer`

### Error reporting queue (machine)  `windows.wer.machine`

- **Category:** Windows
- **Risk:** Safe · **Action:** Delete
- **Why:** Machine-wide Windows Error Reporting queue and archive. Rebuilt when the next problem is reported.
- **Locations:**
  - `{ProgramData}\Microsoft\Windows\WER` → ReportQueue, ReportArchive, Temp

### Error reporting queue  `windows.wer.user`

- **Category:** Windows
- **Risk:** Safe · **Action:** Delete
- **Why:** Queued and archived Windows Error Reporting data for your account. Rebuilt when the next problem is reported.
- **Locations:**
  - `{LocalAppData}\Microsoft\Windows\WER` → ReportQueue, ReportArchive, Temp

## Level 2 — Standard

### Discord cache  `comms.discord.cache`

- **Category:** App & game caches
- **Risk:** Safe · **Action:** Delete
- **Close first:** discord.exe, discordptb.exe, discordcanary.exe
- **Why:** Discord's web cache (Stable, PTB and Canary). Your login, servers and messages are never touched.
- **Locations:**
  - `{RoamingAppData}\discord` → Cache, Code Cache, GPUCache
  - `{RoamingAppData}\discordptb` → Cache, Code Cache, GPUCache
  - `{RoamingAppData}\discordcanary` → Cache, Code Cache, GPUCache

### Slack cache  `comms.slack.cache`

- **Category:** App & game caches
- **Risk:** Safe · **Action:** Delete
- **Close first:** slack.exe
- **Why:** Slack's web cache. Your workspaces, messages and sign-in are never touched.
- **Locations:**
  - `{RoamingAppData}\Slack` → Cache, Code Cache, GPUCache

### Spotify cache  `comms.spotify.cache`

- **Category:** App & game caches
- **Risk:** Safe · **Action:** Delete
- **Close first:** Spotify.exe
- **Why:** Spotify's streaming data cache. Your account, playlists and downloads are never touched.
- **Locations:**
  - `{LocalAppData}\Spotify\Data`

### Microsoft Teams (classic) cache  `comms.teams.classic.cache`

- **Category:** App & game caches
- **Risk:** Safe · **Action:** Delete
- **Close first:** Teams.exe
- **Why:** Classic Teams web cache. Your chats and sign-in are never touched.
- **Locations:**
  - `{RoamingAppData}\Microsoft\Teams` → Cache, Code Cache, GPUCache

### Cursor cache  `dev.cursor.cache`

- **Category:** App & game caches
- **Risk:** Safe · **Action:** Delete
- **Close first:** Cursor.exe
- **Why:** Cursor's cached data. Your settings, extensions and workspaces are never touched.
- **Locations:**
  - `{RoamingAppData}\Cursor` → CachedData, Cache, GPUCache, Code Cache

### VS Code cache  `dev.vscode.cache`

- **Category:** App & game caches
- **Risk:** Safe · **Action:** Delete
- **Close first:** Code.exe
- **Why:** VS Code's cached data. Your settings, extensions and workspaces are never touched.
- **Locations:**
  - `{RoamingAppData}\Code` → CachedData, Cache, GPUCache, Code Cache

### Battle.net cache  `launcher.battlenet.cache`

- **Category:** App & game caches
- **Risk:** Safe · **Action:** Delete
- **Close first:** Battle.net.exe
- **Why:** The Battle.net client's web cache. Your games and account are never touched.
- **Locations:**
  - `{LocalAppData}\Battle.net\Cache`
  - `{ProgramData}\Battle.net\Cache`

### EA app cache  `launcher.ea.cache`

- **Category:** App & game caches
- **Risk:** Safe · **Action:** Delete
- **Close first:** EADesktop.exe
- **Why:** The EA app's embedded-browser cache. Your games, saves and account are never touched.
- **Locations:**
  - `{LocalAppData}\Electronic Arts\EA Desktop\CEF`

### Epic Games Launcher web cache  `launcher.epic.webcache`

- **Category:** App & game caches
- **Risk:** Safe · **Action:** Delete
- **Close first:** EpicGamesLauncher.exe
- **Why:** The Epic launcher's embedded-browser cache. Your games, saves and library are never touched.
- **Locations:**
  - `{LocalAppData}\EpicGamesLauncher\Saved` → webcache, webcache_4147, webcache_4430

### GOG Galaxy web cache  `launcher.gog.webcache`

- **Category:** App & game caches
- **Risk:** Safe · **Action:** Delete
- **Close first:** GalaxyClient.exe
- **Why:** GOG Galaxy's embedded-browser cache. Your games, saves and account are never touched.
- **Locations:**
  - `{ProgramData}\GOG.com\Galaxy\webcache`

### Ubisoft Connect cache  `launcher.ubisoft.cache`

- **Category:** App & game caches
- **Risk:** Safe · **Action:** Delete
- **Close first:** UbisoftConnect.exe, upc.exe
- **Why:** Ubisoft Connect's cache. Your games, saves and account are never touched.
- **Locations:**
  - `{LocalAppData}\Ubisoft Game Launcher\cache`

### Brave cache  `browser.brave.cache`

- **Category:** Browser caches
- **Risk:** Safe · **Action:** Delete
- **Close first:** brave.exe
- **Why:** Cached web files only, for every Brave profile. Passwords, cookies, history and extensions are never touched.
- **Locations:**
  - `{LocalAppData}\BraveSoftware\Brave-Browser\User Data` → Cache, Code Cache, GPUCache
  - `{LocalAppData}\BraveSoftware\Brave-Browser\User Data` → ShaderCache, GrShaderCache, GraphiteDawnCache

### Google Chrome cache  `browser.chrome.cache`

- **Category:** Browser caches
- **Risk:** Safe · **Action:** Delete
- **Close first:** chrome.exe
- **Why:** Cached web files only, for every Chrome profile. Passwords, cookies, history, bookmarks and extensions are never touched.
- **Locations:**
  - `{LocalAppData}\Google\Chrome\User Data` → Cache, Code Cache, GPUCache
  - `{LocalAppData}\Google\Chrome\User Data` → ShaderCache, GrShaderCache, GraphiteDawnCache

### Microsoft Edge cache  `browser.edge.cache`

- **Category:** Browser caches
- **Risk:** Safe · **Action:** Delete
- **Close first:** msedge.exe
- **Why:** Cached web files only, for every Edge profile. Passwords, cookies, history, favorites and extensions are never touched.
- **Locations:**
  - `{LocalAppData}\Microsoft\Edge\User Data` → Cache, Code Cache, GPUCache
  - `{LocalAppData}\Microsoft\Edge\User Data` → ShaderCache, GrShaderCache, GraphiteDawnCache

### Firefox cache  `browser.firefox.cache`

- **Category:** Browser caches
- **Risk:** Safe · **Action:** Delete
- **Close first:** firefox.exe
- **Why:** Firefox's disk and startup caches, for every profile. Passwords, cookies, history, bookmarks and add-ons are never touched.
- **Locations:**
  - `{LocalAppData}\Mozilla\Firefox\Profiles` → cache2, startupCache

### Opera cache  `browser.opera.cache`

- **Category:** Browser caches
- **Risk:** Safe · **Action:** Delete
- **Close first:** opera.exe
- **Why:** Opera's cached web files. Passwords, cookies, history and extensions are never touched.
- **Locations:**
  - `{LocalAppData}\Opera Software\Opera Stable` → Cache, Code Cache, GPUCache

### Opera GX cache  `browser.operagx.cache`

- **Category:** Browser caches
- **Risk:** Safe · **Action:** Delete
- **Close first:** opera.exe
- **Why:** Opera GX's cached web files. Passwords, cookies, history and extensions are never touched.
- **Locations:**
  - `{LocalAppData}\Opera Software\Opera GX Stable` → Cache, Code Cache, GPUCache

### Vivaldi cache  `browser.vivaldi.cache`

- **Category:** Browser caches
- **Risk:** Safe · **Action:** Delete
- **Close first:** vivaldi.exe
- **Why:** Cached web files only, for every Vivaldi profile. Passwords, cookies, history and extensions are never touched.
- **Locations:**
  - `{LocalAppData}\Vivaldi\User Data` → Cache, Code Cache, GPUCache
  - `{LocalAppData}\Vivaldi\User Data` → ShaderCache, GrShaderCache, GraphiteDawnCache

### AMD shader caches  `gpu.amd.caches`

- **Category:** GPU & shader caches
- **Risk:** Safe · **Action:** Delete
- **Why:** AMD's DirectX/Vulkan/OpenGL shader caches. The driver rebuilds them automatically.
- **Locations:**
  - `{LocalAppData}\AMD` → DxCache, DxcCache, GLCache, VkCache

### Intel shader cache  `gpu.intel.shadercache`

- **Category:** GPU & shader caches
- **Risk:** Safe · **Action:** Delete
- **Why:** Intel graphics driver shader cache. Rebuilt automatically by the driver.
- **Locations:**
  - `{LocalAppData}\Intel\ShaderCache`

### NVIDIA shader caches  `gpu.nvidia.caches`

- **Category:** GPU & shader caches
- **Risk:** Safe · **Action:** Delete
- **Why:** NVIDIA's compiled shader and compute caches. The driver rebuilds them automatically; the first launch of a game afterwards may be slightly slower.
- **Locations:**
  - `{LocalAppData}\NVIDIA` → DXCache, GLCache, ComputeCache, OptixCache
  - `{LocalAppData}\NVIDIA Corporation` → NV_Cache

### DirectX shader cache  `windows.directx.shadercache`

- **Category:** GPU & shader caches
- **Risk:** Safe · **Action:** Delete
- **Why:** DirectX's compiled shader cache. Games and apps rebuild it automatically; the first launch afterwards may be slightly slower.
- **Locations:**
  - `{LocalAppData}\D3DSCache`

### Old Windows logs  `windows.logs`

- **Category:** Windows
- **Risk:** Safe · **Action:** Delete
- **Why:** Servicing and setup log files (CBS, DISM, Windows Update) older than 7 days. Diagnostic only; Windows keeps writing fresh ones.
- **Locations:**
  - `{Windows}\Logs`

## Level 3 — Deep

### Adobe media cache  `creative.adobe.mediacache`

- **Category:** App & game caches
- **Risk:** Caution · **Action:** Quarantine
- **Close first:** Adobe Premiere Pro.exe, AfterFX.exe, Adobe Media Encoder.exe
- **Why:** Adobe's conformed media cache (Premiere Pro, After Effects). Rebuilt automatically; projects re-open fine but may take longer the first time. Moved to Quarantine so it can be restored.
- **Locations:**
  - `{RoamingAppData}\Adobe\Common\Media Cache Files`
  - `{RoamingAppData}\Adobe\Common\Media Cache`

### FiveM / RedM cache & logs  `game.fivem.cache`

- **Category:** App & game caches
- **Risk:** Safe · **Action:** Delete
- **Close first:** FiveM.exe, FiveM_b2802_GTAProcess.exe
- **Why:** FiveM/RedM download caches, server caches and logs only. Your game-storage, mods, plugins and citizen folders are never touched.
- **Locations:**
  - `{LocalAppData}\FiveM\FiveM.app` → data\cache, data\server-cache, data\server-cache-priv, logs, crashes

### Temporary files (all accounts)  `windows.temp.allprofiles`

- **Category:** Temporary files
- **Risk:** Safe · **Action:** Delete
- **Why:** The temporary folders of every user account on this PC. Each is scratch space rebuilt on demand; files from the last 24 hours are left alone.
- **Locations:**
  - `{EachUserProfile}\AppData\Local\Temp`

### Windows Update download cache  `windows.update.download`

- **Category:** Windows Update
- **Risk:** Caution · **Action:** Quarantine
- **Why:** Downloaded Windows Update installers that have already been applied. Windows re-downloads anything it still needs. Moved to Quarantine so it can be restored.
- **Locations:**
  - `{Windows}\SoftwareDistribution\Download`

## Level 4 — Expert

### Windows component store cleanup (DISM)  `expert.dism.componentcleanup`

- **Category:** Windows
- **Risk:** Expert · **Action:** Command
- **Why:** Runs DISM StartComponentCleanup to remove superseded Windows update components from WinSxS. It never resets the base, so you keep the ability to uninstall updates. Can take a long time.
- **Operation:** `DismComponentCleanup`

### Clear Windows event logs  `expert.eventlogs`

- **Category:** Windows
- **Risk:** Expert · **Action:** Command
- **Why:** Clears the Windows event logs. This frees a little space but erases troubleshooting history, so it is off by default.
- **Operation:** `ClearEventLogs`

### Prefetch data  `expert.prefetch`

- **Category:** Windows
- **Risk:** Expert · **Action:** Delete
- **Why:** Windows Prefetch records that speed up app and boot launches. Windows rebuilds them, but boot and the first few app launches will be slower for a while.
- **Locations:**
  - `{Windows}\Prefetch`

### Empty the Recycle Bin  `expert.recyclebin`

- **Category:** Windows
- **Risk:** Expert · **Action:** Command
- **Why:** Permanently empties the Recycle Bin on all drives. This cannot be undone, so it is off by default and must be selected explicitly.
- **Operation:** `RecycleBin`

