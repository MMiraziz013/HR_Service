FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HR_Service/HR_Service.csproj", "HR_Service/"]
COPY ["Clean.Application/Clean.Application.csproj", "Clean.Application/"]
COPY ["Clean.Domain/Clean.Domain.csproj", "Clean.Domain/"]
COPY ["Clean.Infrastructure/Clean.Infrastructure.csproj", "Clean.Infrastructure/"]
RUN dotnet restore "HR_Service/HR_Service.csproj"
COPY . .
WORKDIR "/src/HR_Service"
RUN dotnet build "./HR_Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HR_Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HR_Service.dll"]
