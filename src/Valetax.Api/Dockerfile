FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Valetax.sln", "./"]
COPY ["src/Valetax.Api/Valetax.Api.csproj", "src/Valetax.Api/"]
COPY ["src/Valetax.Application/Valetax.Application.csproj", "src/Valetax.Application/"]
COPY ["src/Valetax.Domain/Valetax.Domain.csproj", "src/Valetax.Domain/"]
COPY ["src/Valetax.Infrastructure/Valetax.Infrastructure.csproj", "src/Valetax.Infrastructure/"]

RUN dotnet restore "src/Valetax.Api/Valetax.Api.csproj"

COPY . .

RUN dotnet publish "src/Valetax.Api/Valetax.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Valetax.Api.dll"]
