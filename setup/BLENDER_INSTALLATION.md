# Blender installation receipt

Installed and verified September 5, 2026.

- Version: **Blender 5.2.1 LTS**, build hash `9e2066aef7ef`.
- Application: `%LOCALAPPDATA%\Programs\Blender\blender-5.2.1-windows-x64\blender.exe`.
- Installation type: official portable distribution, installed for this user. It is not an MSI-registered installation.
- Source: https://mirror.blender.org/release/Blender5.2/blender-5.2.1-windows-x64.zip
- SHA256: `0e631dad7d0cad6d5d18abdd2e2550f6c0213215334eda00ddbd3d22b96ecb2c`, matched against the official release checksum file.
- Added its application folder to the existing user PATH, preserving existing entries. Already-running terminals/apps may require restarting to inherit this change; agents can use the absolute executable path immediately.
- Start menu shortcut: `Blender 5.2.1`.
- Existing Blender user settings were preserved. Verification used factory startup settings without saving preferences.

## Verification

The application returned its version successfully. Background execution of `verify_blender.py` exited with code 0 and printed:

> VERIFIED: Blender 5.2.1 LTS; scene saved; FBX export successful

Outputs are `verification/blender_check.blend` and `verification/blender_check.fbx`. This proves Blender-side scene creation and export, not a Unity import or game build.

## Installation issues resolved

The package-manager download returned HTTP 403 from the main download server. The official mirror worked. The MSI checksum and Blender Foundation signature were verified, but the MSI installation returned 1603 / error 1303 because the process could not write to Program Files. The user-level portable package completed without administrator access. The MSI attempt rolled back; its log is `blender-install.log`.

The downloaded MSI, ZIP, checksum file and diagnostic outputs remain in this setup folder. No Meshy generation calls were made.
