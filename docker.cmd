@echo off
setlocal EnableExtensions EnableDelayedExpansion

REM Simple docker->nerdctl shim for Rancher Desktop (containerd)
REM - Forces the k8s.io namespace so images are visible to k3s

set "NS=k8s.io"
if not "%DOCKER_SHIM_NAMESPACE%"=="" set "NS=%DOCKER_SHIM_NAMESPACE%"

set "cmd=%~1"
if "%cmd%"=="" (
  echo docker: missing command
  exit /b 1
)
shift

REM Handle grouped commands like: docker image inspect ...
if /I "%cmd%"=="image" (
  set "sub=%~1"
  shift
  if /I "%sub%"=="inspect" (
    nerdctl --namespace %NS% inspect %*
    exit /b %ERRORLEVEL%
  )
  if /I "%sub%"=="ls" (
    nerdctl --namespace %NS% images %*
    exit /b %ERRORLEVEL%
  )
  nerdctl --namespace %NS% image %sub% %*
  exit /b %ERRORLEVEL%
)

if /I "%cmd%"=="inspect" (
  nerdctl --namespace %NS% inspect %*
  exit /b %ERRORLEVEL%
)

if /I "%cmd%"=="images" (
  nerdctl --namespace %NS% images %*
  exit /b %ERRORLEVEL%
)

if /I "%cmd%"=="build" (
  nerdctl --namespace %NS% build %*
  exit /b %ERRORLEVEL%
)

if /I "%cmd%"=="tag" (
  nerdctl --namespace %NS% tag %*
  exit /b %ERRORLEVEL%
)

if /I "%cmd%"=="push" (
  nerdctl --namespace %NS% push %*
  exit /b %ERRORLEVEL%
)

if /I "%cmd%"=="pull" (
  nerdctl --namespace %NS% pull %*
  exit /b %ERRORLEVEL%
)

if /I "%cmd%"=="rmi" (
  nerdctl --namespace %NS% rmi %*
  exit /b %ERRORLEVEL%
)

if /I "%cmd%"=="info" (
  nerdctl --namespace %NS% info %*
  exit /b %ERRORLEVEL%
)

if /I "%cmd%"=="version" (
  REM Skaffold often calls: docker version --format {{.Server.APIVersion}}
  set "hasFormat=0"
  for %%A in (%*) do (
    if /I "%%~A"=="--format" set "hasFormat=1"
  )
  if "%hasFormat%"=="1" (
    echo 1.0
    exit /b 0
  )
  nerdctl --namespace %NS% version %*
  exit /b %ERRORLEVEL%
)

REM Fallback: try to run the same command in nerdctl
nerdctl --namespace %NS% %cmd% %*
exit /b %ERRORLEVEL%
