# Skyworth VR Bootstrap

Unity Package Manager package for Skyworth S801-like Android VR devices.

This is an adapter around official Skyworth native libraries. It keeps Unity
rendering on the normal Android `SurfaceView` and uses the official
`awGetOrientation(double, float*)` path for head orientation.

See `Documentation~/README.md` for setup and runtime notes.

## Distribution Notice

This package currently includes Skyworth native binaries:

- `libatw_api.so`
- `libavr_api.so`

Before publishing this package publicly, confirm that you have redistribution
rights for those binaries. If not, remove them from the public package and document
where users should obtain the official SDK files.
