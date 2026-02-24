FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Instalar toolchain nativa necessária para AOT
RUN apt-get update && \
    apt-get install -y clang zlib1g-dev

COPY skills-server.cs skills-server.cs

RUN dotnet publish skills-server.cs

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0
WORKDIR /app

COPY skills skills
COPY --from=build /source/artifacts/skills-server ./

ENTRYPOINT ["./skills-server"]