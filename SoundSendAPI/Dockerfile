FROM microsoft/dotnet:2.1-sdk-nanoserver-1803 AS dotnet

# Windows Server image with .NET Framework image as base
FROM microsoft/dotnet-framework:4.7.2-runtime-windowsservercore-1803

# copy the .NET Core stuff from the .NET Core Image to the Windows Server Image
ENV DOTNET_PATH="C:\Program Files\dotnet"
COPY --from=dotnet ${DOTNET_PATH} ${DOTNET_PATH}

# set environment path with .NET Core
RUN $env:PATH = $env:DOTNET_PATH + ';' + $env:PATH; `
    [Environment]::SetEnvironmentVariable('PATH', $env:PATH, [EnvironmentVariableTarget]::Machine)

# Configure web servers to bind to port 80 when present
ENV ASPNETCORE_URLS=http://+:8999 `
    # Enable detection of running in a container
    DOTNET_RUNNING_IN_CONTAINERS=true `
    # ignore first time expierence
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE="true"