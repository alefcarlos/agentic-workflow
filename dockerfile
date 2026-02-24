FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY skills-server.cs skills-server.cs

RUN dotnet publish skills-server.cs

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY skills skills
COPY --from=build /source/artifacts/skills-server ./

ENTRYPOINT ["dotnet", "skills-server.dll"]