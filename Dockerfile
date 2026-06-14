FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore FlowDesk.sln
RUN dotnet publish FlowDesk.API/FlowDesk.API.csproj -c Release -o /app/out --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "FlowDesk.API.dll"]
