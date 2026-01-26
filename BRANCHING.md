# Branch Strategy & Multi-Platform Workflow

This project uses a **multi-platform branching strategy** to support development on both Linux and iOS.

## Branch Structure

```
main          Stable releases only
dev           Linux development (primary)
ios/dev       iOS development
```

## How It Works

| Branch | Purpose | Commits From |
|--------|---------|--------------|
| `main` | Production releases | Merges from dev |
| `dev` | Shared code, Linux-specific work | Linux machine |
| `ios/dev` | Apple platform work (Xcode, iOS-specific code) | macOS machine |

The `ios/dev` branch is essentially `dev` **plus** Apple platform layers. It always contains everything from `dev`, with iOS additions on top.

## Workflow

### On Linux (Primary Development)
```bash
# Work happens on dev branch
git checkout dev
# ... make changes ...
git commit -m "Your changes"
git push origin dev
```

### On macOS (iOS Development)

**Start of session - sync from Linux:**
```bash
git checkout dev           # Switch to shared branch
git pull origin dev        # Get latest Linux work
git checkout ios/dev       # Switch back to iOS branch
git merge dev              # Bring Linux changes into iOS branch
```

**After iOS work is done:**
```bash
git add <files>
git commit -m "[iOS] Your changes"
git push origin ios/dev
```

## Important Rules

1. **Never commit iOS-specific changes to `dev`** - they belong on `ios/dev`
2. **Always sync before starting iOS work** - pull `dev` and merge into `ios/dev`
3. **Prefix iOS commits with `[iOS]`** - makes it clear which commits are platform-specific
4. **The `ios/dev` branch should never be merged back into `dev`** - it contains platform-specific code that doesn't belong in the shared codebase

## Merge Direction

```
dev  ──────────────────────────────►  (continues)
    │
    └──── merge ────►  ios/dev  ────►  (continues)
```

Changes flow **one direction**: from `dev` into `ios/dev`. Never the reverse.

## Handling Merge Conflicts

When merging `dev` into `ios/dev`, conflicts may occur:

1. Git will list conflicting files
2. Open each file and look for conflict markers:
   ```
   <<<<<<< HEAD
   (your iOS changes)
   =======
   (Linux changes)
   >>>>>>>
   ```
3. Edit to keep what you need from both sides
4. `git add <file>`
5. `git commit`

## Quick Reference (macOS Shell Aliases)

If using the project shell aliases:

| Command | Action |
|---------|--------|
| `sync-sq` | Pull dev and merge into ios/dev |
| `push-sq` | Push ios/dev to GitHub |
| `dev-status` | Show git status for all projects |
