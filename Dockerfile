# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet publish src/TaskOrchestrator.Api/TaskOrchestrator.Api.csproj \
    -c Release -o /app/publish --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .

# uploads dir — on Render free tier the filesystem is ephemeral; use a volume or object storage for persistence
RUN mkdir -p uploads

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TaskOrchestrator.Api.dll"]
