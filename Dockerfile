FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NombaCommerceConnect.sln ./
COPY src/NombaCommerceConnect.Domain/NombaCommerceConnect.Domain.csproj src/NombaCommerceConnect.Domain/
COPY src/NombaCommerceConnect.Application/NombaCommerceConnect.Application.csproj src/NombaCommerceConnect.Application/
COPY src/NombaCommerceConnect.Infrastructure/NombaCommerceConnect.Infrastructure.csproj src/NombaCommerceConnect.Infrastructure/
COPY src/NombaCommerceConnect.Api/NombaCommerceConnect.Api.csproj src/NombaCommerceConnect.Api/
COPY tests/NombaCommerceConnect.Tests/NombaCommerceConnect.Tests.csproj tests/NombaCommerceConnect.Tests/

RUN dotnet restore src/NombaCommerceConnect.Api/NombaCommerceConnect.Api.csproj

COPY . .
RUN dotnet publish src/NombaCommerceConnect.Api/NombaCommerceConnect.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 8080

ENTRYPOINT ["dotnet", "NombaCommerceConnect.Api.dll"]
