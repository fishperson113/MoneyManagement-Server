# Base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["API.csproj", "."]
RUN dotnet restore "API.csproj"
COPY . .
COPY Prompts ./Prompts
RUN dotnet build "API.csproj" -c Release -o /app/build

# Publish image
FROM build AS publish
RUN dotnet publish "API.csproj" -c Release -o /app/publish /p:UseAppHost=false
COPY Prompts /app/publish/Prompts

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "API.dll"]
