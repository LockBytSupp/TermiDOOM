
# TermiDOOM

A small Doom-inspired first-person shooter rendered in the terminal, written in C# with ASCII graphics and ANSI colors.

## Features

- Pseudo-3D raycast rendering
- Movement and strafing
- Enemies, shooting, health, and ammo
- Terminal color effects

## Requirements

- C# compiler (`mcs`)
- Mono runtime (`mono`)
- A terminal that supports ANSI escape sequences

## Build and Run

```bash
mcs -out:TermiDOOM.exe Program.cs
mono TermiDOOM.exe
```

## Controls

- `W` / `Up Arrow` — move forward
- `S` / `Down Arrow` — move backward
- `A` / `D` — strafe
- `Left` / `Right Arrow` — turn
- `Space` / `Enter` — shoot
- `R` — reload and heal
- `C` — toggle colors
- `Q` / `Esc` — quit

TermiDOOM is an independent prototype inspired by classic first-person shooters. It is not a port of the original DOOM engine and does not include original DOOM assets.

# Supported OS's
- Android
- Linux
- Windows
