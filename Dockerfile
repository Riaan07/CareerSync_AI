# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CareerSyncAI/CareerSyncAI.csproj", "CareerSyncAI/"]
RUN dotnet restore "CareerSyncAI/CareerSyncAI.csproj"
COPY . .
WORKDIR "/src/CareerSyncAI"
RUN dotnet build "CareerSyncAI.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CareerSyncAI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CareerSyncAI.dll"]
