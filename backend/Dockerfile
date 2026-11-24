# API .NET 10 — build context: thư mục backend/
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props Coaching.slnx ./
COPY src/Coaching.Domain/Coaching.Domain.csproj src/Coaching.Domain/
COPY src/Coaching.Application/Coaching.Application.csproj src/Coaching.Application/
COPY src/Coaching.Infrastructure/Coaching.Infrastructure.csproj src/Coaching.Infrastructure/
COPY src/Coaching.Api/Coaching.Api.csproj src/Coaching.Api/
RUN dotnet restore src/Coaching.Api/Coaching.Api.csproj

COPY src/ src/
RUN dotnet publish src/Coaching.Api/Coaching.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Coaching.Api.dll"]
