FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY observability-flow.csproj ./
RUN dotnet restore observability-flow.csproj

COPY . ./
RUN dotnet publish observability-flow.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "observability-flow.dll"]
