# The EF Core migration bundle for one commit, as a small image: build it from the repo root with
#   docker build -f deploy/migrations.Dockerfile -t <registry>/ecommerce-migrations:<sha> .
# Run it with ConnectionStrings__Database set; it applies every pending migration and exits.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# dotnet-ef comes from the tool manifest, so its version is pinned in the repo.
COPY ["dotnet-tools.json", "."]
RUN dotnet tool restore

COPY ["Directory.Build.props", "Directory.Packages.props", "."]
COPY ["Ecommerce.API/Ecommerce.API.csproj", "Ecommerce.API/"]
COPY ["Ecommerce.Application/Ecommerce.Application.csproj", "Ecommerce.Application/"]
COPY ["Ecommerce.Domain/Ecommerce.Domain.csproj", "Ecommerce.Domain/"]
COPY ["Ecommerce.Infrastructure/Ecommerce.Infrastructure.csproj", "Ecommerce.Infrastructure/"]

RUN dotnet restore "Ecommerce.API/Ecommerce.API.csproj" -r linux-x64

COPY . .

# dotnet ef builds and publishes the project itself. The build-time OpenAPI step boots the app, which has no
# database here, so it is switched off for every MSBuild call through the environment (the csproj only
# defaults the property to true when nothing set it).
ENV OpenApiGenerateDocuments=false

# --self-contained makes a single file: the final image needs no .NET runtime and no SDK.
RUN dotnet ef migrations bundle     --project Ecommerce.Infrastructure     --startup-project Ecommerce.API     --configuration Release     --self-contained -r linux-x64     --output /out/efbundle     --force

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS final

WORKDIR /app

COPY --from=build /out/efbundle .

# The bundle reads ConnectionStrings__Database from the environment, like the API does.
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

USER $APP_UID

ENTRYPOINT ["./efbundle"]
