FROM mcr.microsoft.com/dotnet:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Geniapp.Master/Geniapp.Master.csproj", "Geniapp.Master/"]
RUN dotnet restore "Geniapp.Master/Geniapp.Master.csproj"
COPY . .
WORKDIR "/src/Geniapp.Master"
RUN dotnet build "Geniapp.Master.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Geniapp.Master.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Geniapp.Master.dll"]
