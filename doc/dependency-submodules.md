# Dependency Submodules

| Submodule | Path | Branch |
|-----------|------|--------|
| Bannerlord.Cannons | `src/submodules/Cannons` | `migrate-to-v1.3` |
| Bannerlord.ExpandedTemplate | `src/submodules/ExpandedTemplate` | `upgrade-to-1.3` |

## Clone

Run all git commands from the **repo root** (where `.gitmodules` lives), not from `src/`.

```sh
git clone --recurse-submodules <url>
# or, if already cloned:
git submodule update --init --recursive
```

Then get off detached HEAD:

```sh
git -C src/submodules/Cannons checkout migrate-to-v1.3
git -C src/submodules/ExpandedTemplate checkout upgrade-to-1.3
```

## Change → push in a submodule

```sh
cd src/submodules/Cannons
git checkout -b my-fix   # create a branch first — you're on detached HEAD
git add . && git commit -m "..." && git push

# back in DADG root — record the new pointer
git add src/submodules/Cannons
git commit -m "chore: bump Cannons"
```


## Bump to a new tag

```sh
git -C src/submodules/Cannons fetch --tags
git -C src/submodules/Cannons checkout v1.0.3

git -C src/submodules/ExpandedTemplate fetch --tags
git -C src/submodules/ExpandedTemplate checkout v1.3.2

git commit -m "chore: bump submodules to v1.0.3 / v1.3.2"
```