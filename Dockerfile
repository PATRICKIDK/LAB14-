FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/LAB14PM.Api/LAB14PM.Api.csproj src/LAB14PM.Api/
RUN dotnet restore src/LAB14PM.Api/LAB14PM.Api.csproj
COPY src/LAB14PM.Api/ src/LAB14PM.Api/
RUN dotnet publish src/LAB14PM.Api/LAB14PM.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000
COPY --from=build /app/publish .
ENTRYPOINT ["sh", "-c", "dotnet LAB14PM.Api.dll --urls http://0.0.0.0:${PORT:-10000}"]

