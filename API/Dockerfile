# Giai đoạn cơ sở
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# Giai đoạn xây dựng
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["API.csproj", "."]
RUN dotnet restore "API.csproj"
COPY . .
RUN dotnet build "API.csproj" -c Release -o /app/build

# Giai đoạn xuất bản
FROM build AS publish
RUN dotnet publish "API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Giai đoạn cuối cùng
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "API.dll"]
