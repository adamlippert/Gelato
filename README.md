# Chocolate Gelato — Jellyfin plugin repository

This branch is generated. It hosts the Jellyfin plugin manifest for
[Chocolate Gelato](https://github.com/survivalizer/Gelato), a fork of
[lostb1t/Gelato](https://github.com/lostb1t/Gelato).

## Installing

In Jellyfin, go to **Dashboard → Plugins → Repositories** and add:

```
https://raw.githubusercontent.com/survivalizer/Gelato/refs/heads/gh-pages/repository.json
```

Chocolate Gelato uses its own plugin GUID, so it installs alongside
upstream Gelato rather than replacing it.

## Do not edit by hand

`repository.json` is regenerated on every published release by the
`generate` job in `.github/workflows/publish.yml`, via
[Kevinjil/jellyfin-plugin-repo-action](https://github.com/Kevinjil/jellyfin-plugin-repo-action).
Manual edits will be overwritten.

