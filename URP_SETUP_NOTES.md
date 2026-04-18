# URP Setup Notes for Barn Swarm Sniper

## Project Configuration
- **Unity Version**: 2023 LTS
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Target Platform**: Android (Landscape Left)

## URP Asset Settings
1. **General**:
   - Depth Texture: Enabled (Required for some scope effects)
   - Opaque Texture: Enabled
2. **Quality**:
   - Anti-aliasing (MSAA): 2x or 4x (depending on device performance)
   - Render Scale: 1.0
3. **Lighting**:
   - Main Light: Pixel
   - Additional Lights: Per Pixel (Max 4)
   - Shadowmask: Supported

## Renderer Features
- **Scope Overlay**: Use a custom Scriptable Renderer Feature or a dedicated UI Canvas for the scope vignette and reticle.
- **Night Vision / Thermal**: 
  - Implement using Post-Processing (Volume) or a custom Full Screen Pass Renderer Feature.
  - **NV**: Green tint, noise, slight bloom.
  - **Thermal**: Color mapping (White Hot / Green Hot) based on a custom "Heat" value or simply using a simplified color replacement shader.

## Optimization for Android
- **Texture Compression**: ASTC (preferred) or ETC2.
- **Shader Stripping**: Ensure unused URP features are stripped to reduce build size and load times.
- **Static Batching**: Enable for barn environment pieces.
