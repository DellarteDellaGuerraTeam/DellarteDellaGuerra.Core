# Dependency Submodules

| Submodule | Path | Pinned tag |
|-----------|------|------------|
| Bannerlord.Cannons | `src/submodules/Cannons` | `v1.0.2` |
| Bannerlord.ExpandedTemplate | `src/submodules/ExpandedTemplate` | `v1.3.1` |

Both tags target Bannerlord v1.2.12. Submodules are pinned to a specific commit
SHA — they do not track a branch and will not advance automatically.

## Clone

```sh
git clone --recurse-submodules <url>
# or, if already cloned:
git submodule update --init --recursive
```

No extra checkout needed — the recorded commit is checked out automatically
(detached HEAD at the pinned tag).

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

git add src/submodules/Cannons src/submodules/ExpandedTemplate
git commit -m "chore: bump submodules to v1.0.3 / v1.3.2"
```
