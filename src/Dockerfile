FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Src/Buffalo/src/TTI.Buffalo.ApiRest.Service/TTI.Buffalo.ApiRest.Service.csproj", "Src/Buffalo/src/TTI.Buffalo.ApiRest.Service/"]
COPY ["Src/Buffalo/src/TTI.Buffalo.AmazonS3/TTI.Buffalo.AmazonS3.csproj", "Src/Buffalo/src/TTI.Buffalo.AmazonS3/"]
COPY ["Src/Buffalo/src/TTI.Buffalo/TTI.Buffalo.csproj", "Src/Buffalo/src/TTI.Buffalo/"]
COPY ["Src/Buffalo/src/TTI.Buffalo.GoogleCloud/TTI.Buffalo.GoogleCloud.csproj", "Src/Buffalo/src/TTI.Buffalo.GoogleCloud/"]
RUN dotnet restore "Src/Buffalo/src/TTI.Buffalo.ApiRest.Service/TTI.Buffalo.ApiRest.Service.csproj"
COPY . .
WORKDIR "/src/Src/Buffalo/src/TTI.Buffalo.ApiRest.Service"
RUN dotnet build "TTI.Buffalo.ApiRest.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TTI.Buffalo.ApiRest.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TTI.Buffalo.ApiRest.Service.dll"]
