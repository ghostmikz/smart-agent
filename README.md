# SmartSoftware Device Agent

Small Windows service. It runs on a store PC and lets the web apps talk to local devices over HTTP.

## What it controls

- Barcode scales (Aclas, CAS)
- IP cameras (NX Witness)
- Electronic price tags
- Zebra RFID printers

## Run

- .NET 10, Windows, x86.
- Listens on `http://0.0.0.0:6202` (see `appsettings.json`).
- Health check: `GET /health`.
- Installs as the Windows service `Smart Agent Worker`.

## Build

```
dotnet build SmartSoftware.DeviceAgent.csproj
```

## Notes

- `Libs\` holds vendor files (Aclas SDK, CAS scale OCX). Keep this repo private.
