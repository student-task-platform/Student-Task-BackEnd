# ================================
# Build stage
# ================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (better caching)
COPY Student-Task.csproj ./
RUN dotnet restore ./Student-Task.csproj

# Copy the rest of the source
COPY . .

# Publish the app
RUN dotnet publish ./Student-Task.csproj -c Release -o /app/publish --no-restore

# ================================
# Runtime stage
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

# Render recommends binding to PORT (default 10000) on 0.0.0.0 :contentReference[oaicite:0]{index=0}
ENV PORT=10000
EXPOSE 10000

# Use PORT at runtime (so it works on Render and locally)
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT} dotnet Student-Task.dll"]
