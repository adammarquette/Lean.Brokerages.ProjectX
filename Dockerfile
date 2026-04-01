# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln .
COPY QuantConnect.ProjectXBrokerage/*.csproj ./QuantConnect.ProjectXBrokerage/
COPY QuantConnect.ProjectXBrokerage.Tests/*.csproj ./QuantConnect.ProjectXBrokerage.Tests/
COPY QuantConnect.ProjectXBrokerage.ToolBox/*.csproj ./QuantConnect.ProjectXBrokerage.ToolBox/
COPY ApiInspector/*.csproj ./ApiInspector/

# Copy the LEAN engine dependency
RUN git clone https://github.com/adammarquette/Lean.git /Lean

# Restore dependencies
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet build -c Release --no-restore

FROM build AS test
WORKDIR /src/QuantConnect.ProjectXBrokerage.Tests
CMD ["dotnet", "test", "-c", "Release", "--no-build"]

FROM build AS publish
WORKDIR /src
RUN dotnet publish -c Release --no-build -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "QuantConnect.Brokerages.ProjectXBrokerage.ToolBox.dll"]
