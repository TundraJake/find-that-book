# syntax=docker/dockerfile:1
# Single image: ASP.NET Core API serving the built React client from wwwroot.

# ---- 1. Build the React client ----
FROM node:22-alpine AS client-build
WORKDIR /client
COPY client/package.json client/package-lock.json ./
RUN npm ci
COPY client/ ./
RUN npm run build

# ---- 2. Restore and publish the .NET API ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /source
COPY global.json Directory.Build.props .editorconfig ./
COPY src/FindThatBook.Domain/FindThatBook.Domain.csproj src/FindThatBook.Domain/
COPY src/FindThatBook.Application/FindThatBook.Application.csproj src/FindThatBook.Application/
COPY src/FindThatBook.Infrastructure/FindThatBook.Infrastructure.csproj src/FindThatBook.Infrastructure/
COPY src/FindThatBook.Api/FindThatBook.Api.csproj src/FindThatBook.Api/
RUN dotnet restore src/FindThatBook.Api/FindThatBook.Api.csproj
COPY src/ src/
RUN dotnet publish src/FindThatBook.Api/FindThatBook.Api.csproj --configuration Release --no-restore --output /app/publish

# ---- 3. Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=api-build /app/publish ./
COPY --from=client-build /client/dist ./wwwroot
# Search audit log folder (SearchAudit:Directory); the app runs as a non-root user that cannot create folders under /app.
RUN mkdir -p /app/logs/search-audit && chown -R $APP_UID /app/logs
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "FindThatBook.Api.dll"]
