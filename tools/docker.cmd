@echo off
setlocal

REM Shim for running Skaffold with Rancher Desktop in containerd mode.
REM Skaffold can be configured to use the `docker` CLI; Rancher Desktop (containerd)
REM does not provide a Docker Engine socket, so we forward docker CLI calls to nerdctl.

REM Skaffold sometimes probes docker contexts; nerdctl doesn't support that.
REM Return success with empty output so Skaffold falls back gracefully.
if /i "%~1"=="context" (
  if /i "%~2"=="inspect" exit /b 0
)

set "NERDCTL_EXE=C:\Program Files\Rancher Desktop\resources\resources\win32\bin\nerdctl.exe"

if exist "%NERDCTL_EXE%" (
  "%NERDCTL_EXE%" --namespace k8s.io %*
) else (
  nerdctl --namespace k8s.io %*
)
